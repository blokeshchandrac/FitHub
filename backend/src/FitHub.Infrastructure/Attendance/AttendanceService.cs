using System.Security.Cryptography;
using System.Text;
using FitHub.Application.Attendance;
using FitHub.Domain.Attendance;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.AttendanceServices;

internal sealed class AttendanceService(FitHubDbContext dbContext, TimeProvider timeProvider) : IAttendanceService
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
}
