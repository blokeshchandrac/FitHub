using System.Security.Cryptography;
using System.Text;
using FitHub.Application.Attendance;
using FitHub.Application.Equipment;
using FitHub.Domain.Attendance;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.AttendanceServices;

internal sealed class AttendanceService(FitHubDbContext dbContext, ICardUidHasher cardUidHasher, TimeProvider timeProvider) : IAttendanceService
{
    public async Task<CheckInTokenDto> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var active = await dbContext.Memberships.AnyAsync(item =>
            item.UserId == userId && item.Status == MembershipStatus.Active && item.StartsAt <= now && item.EndsAt > now,
            cancellationToken);
        if (!active) throw new InvalidOperationException("An active membership is required to check in.");

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = now.AddMinutes(2);
        dbContext.AttendanceCheckInTokens.Add(new AttendanceCheckInToken(userId, Hash(rawToken), expiresAt));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CheckInTokenDto(rawToken, expiresAt);
    }

    public async Task<CheckInResultDto> CheckInAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
        {
            throw new InvalidOperationException("The check-in token is invalid.");
        }

        var token = await dbContext.AttendanceCheckInTokens.SingleOrDefaultAsync(
            item => item.TokenHash == Hash(rawToken), cancellationToken)
            ?? throw new InvalidOperationException("The check-in token is invalid.");
        var now = timeProvider.GetUtcNow();
        token.Use(now);

        if (!await dbContext.Users.AnyAsync(item => item.Id == token.UserId && item.IsActive, cancellationToken))
            throw new InvalidOperationException("The member account is disabled.");

        var membershipActive = await dbContext.Memberships.AnyAsync(item =>
            item.UserId == token.UserId && item.Status == MembershipStatus.Active && item.StartsAt <= now && item.EndsAt > now,
            cancellationToken);
        if (!membershipActive) throw new InvalidOperationException("The member does not have an active membership.");

        var attendance = new Domain.Attendance.Attendance(token.UserId, now);
        dbContext.Attendances.Add(attendance);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The check-in token has already been used.");
        }

        return new CheckInResultDto(attendance.Id, attendance.UserId, attendance.CheckedInAt);
    }

    public async Task<AccessValidationDto> ValidateAccessAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Trim().Length > 200)
            return Result(AccessValidationCodes.InvalidToken, "The token or card is not recognized.");

        var value = rawToken.Trim();
        var cardHash = cardUidHasher.Hash(value);
        var card = await dbContext.MemberAccessCards.AsNoTracking().Include(x => x.User)
            .SingleOrDefaultAsync(x => x.UidHash == cardHash, cancellationToken);
        if (card is not null)
        {
            if (!card.User.IsActive) return UserResult(AccessValidationCodes.UserDisabled, "The member account is disabled.", "Card", card.User);
            if (!card.IsActive) return UserResult(AccessValidationCodes.Blocked, "The membership card is blocked.", "Card", card.User);
            return await MembershipResultAsync(card.User, "Card", cancellationToken);
        }

        var token = await dbContext.AttendanceCheckInTokens.AsNoTracking().Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == Hash(value), cancellationToken);
        if (token is null || token.UsedAt is not null)
            return Result(AccessValidationCodes.InvalidToken, "The token or card is not recognized.");
        if (!token.User.IsActive) return UserResult(AccessValidationCodes.UserDisabled, "The member account is disabled.", "QR token", token.User);
        if (token.ExpiresAt <= timeProvider.GetUtcNow())
            return UserResult(AccessValidationCodes.Expired, "The temporary check-in token has expired.", "QR token", token.User);
        return await MembershipResultAsync(token.User, "QR token", cancellationToken);
    }

    public async Task<AttendanceSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var utcToday = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)utcToday.DayOfWeek + 6) % 7;
        var weekStart = new DateTimeOffset(utcToday.AddDays(-daysSinceMonday), TimeSpan.Zero);
        var monthStart = new DateTimeOffset(new DateTime(utcToday.Year, utcToday.Month, 1), TimeSpan.Zero);

        var currentMembership = await dbContext.Memberships.AsNoTracking()
            .Include(item => item.MembershipPlan)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);
        var query = dbContext.Attendances.AsNoTracking().Where(item => item.UserId == userId);
        var weekVisits = await query.CountAsync(item => item.CheckedInAt >= weekStart, cancellationToken);
        var monthVisits = await query.CountAsync(item => item.CheckedInAt >= monthStart, cancellationToken);
        var membershipVisits = currentMembership is null
            ? 0
            : await query.CountAsync(item => item.CheckedInAt >= currentMembership.StartsAt && item.CheckedInAt < currentMembership.EndsAt, cancellationToken);
        var history = await query.OrderByDescending(item => item.CheckedInAt).Take(100)
            .Select(item => new AttendanceDto(item.Id, item.CheckedInAt, item.CheckedOutAt))
            .ToListAsync(cancellationToken);
        var membershipCost = currentMembership is null
            ? 0m
            : await dbContext.Payments.AsNoTracking()
                .Where(item => item.MembershipId == currentMembership.Id && item.Status == PaymentStatus.Succeeded)
                .SumAsync(item => item.Amount, cancellationToken);
        var costPerVisit = currentMembership is null || membershipCost <= 0
            ? null
            : MembershipCostCalculator.Calculate(membershipCost, membershipVisits);

        return new AttendanceSummaryDto(weekVisits, monthVisits, membershipVisits, costPerVisit, history);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private async Task<AccessValidationDto> MembershipResultAsync(
        Domain.Identity.ApplicationUser user, string credentialType, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var membership = await dbContext.Memberships.AsNoTracking().Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.EndsAt).FirstOrDefaultAsync(cancellationToken);
        if (membership is null)
            return UserResult(AccessValidationCodes.MembershipInactive, "The member has no active membership.", credentialType, user);
        if (membership.EndsAt <= now || membership.Status == MembershipStatus.Expired)
            return UserResult(AccessValidationCodes.Expired, "The membership has expired.", credentialType, user, membership.EndsAt);
        if (!membership.IsActiveAt(now))
            return UserResult(AccessValidationCodes.MembershipInactive, "The membership is not active.", credentialType, user, membership.EndsAt);
        return UserResult(AccessValidationCodes.Valid, "Access credential is valid.", credentialType, user, membership.EndsAt);
    }

    private static AccessValidationDto UserResult(string code,string message,string type,Domain.Identity.ApplicationUser user,DateTimeOffset? expires=null) =>
        new(code,message,type,user.Id,(user.PreferredName ?? (user.FirstName + " " + user.LastName)).Trim(),expires);
    private static AccessValidationDto Result(string code,string message) => new(code,message,null,null,null,null);
}
