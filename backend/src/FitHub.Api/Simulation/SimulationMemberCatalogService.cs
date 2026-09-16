using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Api.Simulation;

public sealed class SimulationMemberCatalogService(FitHubDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<IReadOnlyCollection<SimulationMemberResponse>> GetMembersAsync(CancellationToken cancellationToken)
    {
        var memberRoleId = await dbContext.Roles.AsNoTracking()
            .Where(role => role.Name == "Member")
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (memberRoleId is null) return [];

        var memberIds = await dbContext.UserRoles.AsNoTracking()
            .Where(userRole => userRole.RoleId == memberRoleId.Value)
            .Select(userRole => userRole.UserId)
            .ToListAsync(cancellationToken);
        if (memberIds.Count == 0) return [];

        var users = await dbContext.Users.AsNoTracking()
            .Where(user => memberIds.Contains(user.Id))
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ToListAsync(cancellationToken);
        var memberships = await dbContext.Memberships.AsNoTracking()
            .Where(membership => memberIds.Contains(membership.UserId))
            .OrderByDescending(membership => membership.EndsAt)
            .ToListAsync(cancellationToken);
        var cards = await dbContext.MemberAccessCards.AsNoTracking()
            .Where(card => memberIds.Contains(card.UserId))
            .OrderByDescending(card => card.CreatedAt)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        return users.Select(user =>
        {
            var userMemberships = memberships.Where(item => item.UserId == user.Id).ToArray();
            var membership = userMemberships.FirstOrDefault(item => item.IsActiveAt(now)) ?? userMemberships.FirstOrDefault();
            var card = cards.FirstOrDefault(item => item.UserId == user.Id);
            return Map(user, membership, card, now);
        }).ToArray();
    }

    public async Task<SimulationMemberSnapshot?> FindByCardIdAsync(string cardId, CancellationToken cancellationToken)
    {
        var members = await GetMembersAsync(cancellationToken);
        var member = members.FirstOrDefault(item => string.Equals(item.CardId, cardId, StringComparison.Ordinal));
        return member is null
            ? null
            : new SimulationMemberSnapshot(
                member.MemberId,
                member.DisplayName,
                member.CardId,
                member.MembershipStatus,
                member.CardStatus,
                member.AccountStatus);
    }

    private static SimulationMemberResponse Map(
        ApplicationUser user,
        Membership? membership,
        MemberAccessCard? card,
        DateTimeOffset now)
    {
        var displayName = !string.IsNullOrWhiteSpace(user.PreferredName)
            ? user.PreferredName
            : string.Join(' ', new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(displayName)) displayName = "FitHub Member";

        var cardId = card is null ? $"SIM-CARD-{user.Id:N}" : card.Id.ToString("D");
        return new SimulationMemberResponse(
            user.Id,
            displayName,
            EffectiveMembershipStatus(membership, now),
            cardId,
            card is null || card.IsActive ? "ACTIVE" : "BLOCKED",
            user.IsActive ? "ACTIVE" : "SUSPENDED",
            card is null ? "SIMULATION_TOKEN" : "FIT_HUB_CARD");
    }

    private static string EffectiveMembershipStatus(Membership? membership, DateTimeOffset now)
    {
        if (membership is null) return "NONE";
        if (membership.IsActiveAt(now)) return "ACTIVE";
        if (membership.Status == MembershipStatus.Active && membership.EndsAt <= now) return "EXPIRED";
        return membership.Status switch
        {
            MembershipStatus.Pending => "PENDING_PAYMENT",
            MembershipStatus.Paused => "PAUSED",
            MembershipStatus.Expired => "EXPIRED",
            MembershipStatus.Cancelled => "CANCELLED",
            _ => "INACTIVE"
        };
    }
}
