using FitHub.Application.Administration;
using FitHub.Application.Common;
using FitHub.Domain.Auditing;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Administration;

internal sealed class AdministrationManagementService(
    FitHubDbContext db,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : IAdministrationManagementService
{
    public async Task<IReadOnlyCollection<AdminUserSummaryDto>> ListUsersAsync(
        string? search, string? role, bool? active, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();
        if (active.HasValue) query = query.Where(x => x.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => (x.Email ?? "").ToLower().Contains(term) ||
                x.FirstName.ToLower().Contains(term) || x.LastName.ToLower().Contains(term) ||
                (x.PreferredName ?? "").ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            query = from user in query
                    join userRole in db.UserRoles on user.Id equals userRole.UserId
                    join identityRole in db.Roles on userRole.RoleId equals identityRole.Id
                    where identityRole.Name == roleName
                    select user;
        }
        var users = await query.OrderByDescending(x => x.CreatedAt).Take(250).ToListAsync(ct);
        return await MapUsersAsync(users, ct);
    }

    public async Task<AdminUserDetailsDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.PreferredGymBranch).SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return null;
        var summary = (await MapUsersAsync([user], ct)).Single();
        var cards = await db.MemberAccessCards.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminAccessCardDto(x.Id, x.IsActive, x.CreatedAt, x.UpdatedAt)).ToListAsync(ct);
        return new(summary, user.PreferredName, user.DateOfBirth, user.HeightCm, user.WeightKg,
            user.FitnessGoal.ToString(), user.ActivityLevel.ToString(), user.ExperienceLevel?.ToString(),
            user.GetPlannedTrainingDays().Select(day => day.ToString()).ToArray(), user.PreferredGymBranchId,
            user.PreferredGymBranch?.Name, user.OnboardingCompleted, cards);
    }

    public async Task<bool> SetUserActiveAsync(Guid administratorId, Guid userId, bool active, CancellationToken ct = default)
    {
        if (administratorId == userId) throw Validation("userId", "Administrators cannot disable their own account.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return false;
        user.IsActive = active;
        if (!active)
        {
            var now = timeProvider.GetUtcNow();
            var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct);
            foreach (var token in tokens) token.Revoke(now, "administrator");
        }
        AddAudit(administratorId, active ? "UserEnabled" : "UserBlocked", "ApplicationUser", userId,
            active ? "Administrator enabled user access." : "Administrator blocked user access.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetMembershipEnabledAsync(Guid administratorId, Guid membershipId, bool enabled, CancellationToken ct = default)
    {
        var membership = await db.Memberships.SingleOrDefaultAsync(x => x.Id == membershipId, ct);
        if (membership is null) return false;
        if (enabled) membership.Reactivate(timeProvider.GetUtcNow()); else membership.Pause();
        AddAudit(administratorId, enabled ? "MembershipEnabled" : "MembershipDisabled", "Membership", membershipId,
            enabled ? "Administrator re-enabled membership access." : "Administrator disabled membership access.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetCardActiveAsync(Guid administratorId, Guid cardId, bool active, CancellationToken ct = default)
    {
        var card = await db.MemberAccessCards.SingleOrDefaultAsync(x => x.Id == cardId, ct);
        if (card is null) return false;
        if (active) card.Unblock(timeProvider.GetUtcNow()); else card.Block(timeProvider.GetUtcNow());
        AddAudit(administratorId, active ? "AccessCardUnblocked" : "AccessCardBlocked", "MemberAccessCard", cardId,
            active ? "Administrator unblocked member access card." : "Administrator blocked member access card.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<IReadOnlyCollection<AdminUserSummaryDto>> ListTrainersAsync(CancellationToken ct = default) =>
        ListUsersAsync(null, "Trainer", null, ct);

    public async Task<AdminUserSummaryDto> CreateTrainerAsync(Guid administratorId, CreateTrainerCommand command, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(command.Email) is not null)
            throw Validation("email", "A user with this email already exists.");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = command.Email.Trim(), Email = command.Email.Trim(),
            FirstName = command.FirstName.Trim(), LastName = command.LastName.Trim(), IsActive = true
        };
        var create = await userManager.CreateAsync(user, command.TemporaryPassword);
        if (!create.Succeeded) throw new CommandValidationException(new Dictionary<string, string[]>
        {
            ["temporaryPassword"] = create.Errors.Select(x => x.Description).ToArray()
        });
        var role = await userManager.AddToRoleAsync(user, "Trainer");
        if (!role.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw Validation("role", "Trainer account could not be created.");
        }
        AddAudit(administratorId, "TrainerCreated", "ApplicationUser", user.Id, "Administrator created trainer account.");
        await db.SaveChangesAsync(ct);
        return (await MapUsersAsync([user], ct)).Single();
    }

    public async Task<IReadOnlyCollection<SystemActivityDto>> ListActivityAsync(CancellationToken ct = default) =>
        await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(100)
            .Select(x => new SystemActivityDto(x.Id, x.UserId, x.Action, x.EntityType, x.EntityId, x.Details, x.OccurredAt))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<AdminPaymentDto>> ListPaymentsAsync(CancellationToken ct = default) =>
        await db.Payments.AsNoTracking().Include(x => x.User).Include(x => x.MembershipPlan)
            .OrderByDescending(x => x.CreatedAt).Take(500)
            .Select(x => new AdminPaymentDto(x.Id, x.User.Email ?? string.Empty, x.MembershipPlan.Name,
                x.Amount, x.RefundedAmount, x.Amount - x.RefundedAmount, x.Currency, x.Status.ToString(),
                x.CreatedAt, x.PaidAt, x.ProviderReference, x.StripePaymentIntentId, x.IsSeededDemo))
            .ToListAsync(ct);

    private async Task<IReadOnlyCollection<AdminUserSummaryDto>> MapUsersAsync(IReadOnlyCollection<ApplicationUser> users, CancellationToken ct)
    {
        if (users.Count == 0) return [];
        var ids = users.Select(x => x.Id).ToArray();
        var roleRows = await (from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name! }).ToListAsync(ct);
        var roles = roleRows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Select(y => y.Role).Order().ToArray());
        var memberships = (await db.Memberships.AsNoTracking().Where(x => ids.Contains(x.UserId))
            .OrderByDescending(x => x.EndsAt).ToListAsync(ct)).GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.First());
        var cardCounts = await db.MemberAccessCards.AsNoTracking().Where(x => ids.Contains(x.UserId) && x.IsActive)
            .GroupBy(x => x.UserId).Select(x => new { UserId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count, ct);
        var now = timeProvider.GetUtcNow();
        return users.Select(user =>
        {
            memberships.TryGetValue(user.Id, out var membership);
            var status = membership is null ? "None" : membership.IsActiveAt(now) ? "Active" : membership.Status.ToString();
            return new AdminUserSummaryDto(user.Id, user.Email ?? string.Empty,
                (user.PreferredName ?? (user.FirstName + " " + user.LastName)).Trim(),
                roles.GetValueOrDefault(user.Id, []), user.IsActive, status, membership?.Id,
                cardCounts.GetValueOrDefault(user.Id), user.CreatedAt);
        }).ToList();
    }

    private void AddAudit(Guid administratorId, string action, string entityType, Guid entityId, string details) =>
        db.AuditLogs.Add(new AuditLog { UserId = administratorId, Action = action, EntityType = entityType,
            EntityId = entityId, Details = details, OccurredAt = timeProvider.GetUtcNow() });

    private static CommandValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
