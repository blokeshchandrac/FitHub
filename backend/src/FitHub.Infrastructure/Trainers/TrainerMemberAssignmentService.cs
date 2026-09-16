using FitHub.Application.Common;
using FitHub.Application.Profiles;
using FitHub.Application.Trainers;
using FitHub.Domain.Auditing;
using FitHub.Domain.Identity;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Trainers;

internal sealed class TrainerMemberAssignmentService(
    FitHubDbContext db,
    UserManager<ApplicationUser> userManager,
    IProfileService profileService,
    TimeProvider timeProvider) : ITrainerMemberAssignmentService, ITrainerMemberAccessService
{
    public async Task<IReadOnlyCollection<TrainerMemberAssignmentDto>> GetAssignmentsAsync(
        Guid trainerId, bool includeEnded, CancellationToken cancellationToken = default)
    {
        var query = db.TrainerMemberAssignments.AsNoTracking()
            .Include(item => item.Trainer)
            .Include(item => item.Member)
            .Include(item => item.AssignedByAdministrator)
            .Include(item => item.UnassignedByAdministrator)
            .Where(item => item.TrainerId == trainerId);
        if (!includeEnded) query = query.Where(item => item.Status == TrainerMemberAssignmentStatus.Active);
        return (await query.OrderByDescending(item => item.AssignedAt).ToListAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<IReadOnlyCollection<EligibleMemberDto>> SearchEligibleMembersAsync(
        Guid trainerId, string? search, CancellationToken cancellationToken = default)
    {
        var memberRoleId = await db.Roles.Where(role => role.Name == "Member").Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var assignedIds = db.TrainerMemberAssignments.Where(item => item.TrainerId == trainerId && item.Status == TrainerMemberAssignmentStatus.Active)
            .Select(item => item.MemberId);
        var query = from user in db.Users.AsNoTracking()
                    join userRole in db.UserRoles on user.Id equals userRole.UserId
                    where userRole.RoleId == memberRoleId && user.IsActive && !assignedIds.Contains(user.Id)
                    select user;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(user => (user.Email ?? string.Empty).ToLower().Contains(term)
                || user.FirstName.ToLower().Contains(term) || user.LastName.ToLower().Contains(term)
                || (user.PreferredName ?? string.Empty).ToLower().Contains(term));
        }
        return await query.OrderBy(user => user.LastName).ThenBy(user => user.FirstName).Take(100)
            .Select(user => new EligibleMemberDto(user.Id,
                (user.PreferredName ?? (user.FirstName + " " + user.LastName)).Trim(), user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainerMemberAssignmentDto> AssignAsync(
        Guid administratorId, Guid trainerId, Guid memberId, CancellationToken cancellationToken = default)
    {
        if (trainerId == memberId) throw Validation("memberId", "A trainer cannot be assigned to themselves.");
        var trainer = await userManager.FindByIdAsync(trainerId.ToString());
        if (trainer is null || !await userManager.IsInRoleAsync(trainer, "Trainer"))
            throw Validation("trainerId", "Select a valid trainer.");
        if (!trainer.IsActive) throw Validation("trainerId", "A disabled trainer cannot receive assignments.");
        var member = await userManager.FindByIdAsync(memberId.ToString());
        if (member is null || !await userManager.IsInRoleAsync(member, "Member"))
            throw Validation("memberId", "Select a valid member.");
        if (!member.IsActive) throw Validation("memberId", "A disabled member cannot receive assignments.");
        if (await IsAssignedAsync(trainerId, memberId, cancellationToken))
            throw Validation("memberId", "This member is already assigned to the trainer.");
        var administrator = await userManager.FindByIdAsync(administratorId.ToString())
            ?? throw Validation("administratorId", "The administrator account is unavailable.");

        var assignment = new TrainerMemberAssignment(trainerId, memberId, administratorId, timeProvider.GetUtcNow());
        db.TrainerMemberAssignments.Add(assignment);
        AddAudit(administratorId, "TrainerMemberAssigned", assignment.Id,
            $"Trainer {trainerId} was assigned to member {memberId}.");
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw Validation("memberId", "This member is already assigned to the trainer.");
        }

        return new(assignment.Id, trainerId, DisplayName(trainer), memberId, DisplayName(member), member.Email ?? string.Empty,
            assignment.Status.ToString(), assignment.AssignedAt, administratorId, DisplayName(administrator), null, null, null);
    }

    public async Task<bool> UnassignAsync(
        Guid administratorId, Guid trainerId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await db.TrainerMemberAssignments.SingleOrDefaultAsync(item =>
            item.Id == assignmentId && item.TrainerId == trainerId, cancellationToken);
        if (assignment is null) return false;
        assignment.End(administratorId, timeProvider.GetUtcNow());
        AddAudit(administratorId, "TrainerMemberUnassigned", assignment.Id,
            $"Trainer {assignment.TrainerId} was unassigned from member {assignment.MemberId}.");
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> IsAssignedAsync(Guid trainerId, Guid memberId, CancellationToken cancellationToken = default) =>
        db.TrainerMemberAssignments.AsNoTracking().AnyAsync(item => item.TrainerId == trainerId
            && item.MemberId == memberId && item.Status == TrainerMemberAssignmentStatus.Active
            && item.Trainer.IsActive && item.Member.IsActive, cancellationToken);

    public async Task EnsureAssignedAsync(Guid trainerId, Guid memberId, CancellationToken cancellationToken = default)
    {
        if (!await IsAssignedAsync(trainerId, memberId, cancellationToken))
            throw new UnauthorizedAccessException("The member is not assigned to this trainer.");
    }

    public async Task<IReadOnlyCollection<AssignedMemberDto>> GetAssignedMembersAsync(
        Guid trainerId, CancellationToken cancellationToken = default)
    {
        var assignments = await db.TrainerMemberAssignments.AsNoTracking().Include(item => item.Member)
            .Where(item => item.TrainerId == trainerId && item.Status == TrainerMemberAssignmentStatus.Active && item.Member.IsActive)
            .OrderBy(item => item.Member.LastName).ThenBy(item => item.Member.FirstName).ToListAsync(cancellationToken);
        var result = new List<AssignedMemberDto>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var profile = await profileService.GetAsync(assignment.MemberId, cancellationToken);
            if (profile is not null)
                result.Add(new AssignedMemberDto(assignment.MemberId, DisplayName(assignment.Member),
                    assignment.Member.Email ?? string.Empty, assignment.AssignedAt, profile));
        }
        return result;
    }

    private void AddAudit(Guid administratorId, string action, Guid assignmentId, string details) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = administratorId,
            Action = action,
            EntityType = nameof(TrainerMemberAssignment),
            EntityId = assignmentId,
            Details = details,
            OccurredAt = timeProvider.GetUtcNow()
        });

    private static TrainerMemberAssignmentDto Map(TrainerMemberAssignment item) => new(
        item.Id, item.TrainerId, DisplayName(item.Trainer), item.MemberId, DisplayName(item.Member),
        item.Member.Email ?? string.Empty, item.Status.ToString(), item.AssignedAt,
        item.AssignedByAdministratorId, DisplayName(item.AssignedByAdministrator), item.UnassignedAt,
        item.UnassignedByAdministratorId, item.UnassignedByAdministrator is null ? null : DisplayName(item.UnassignedByAdministrator));

    private static string DisplayName(ApplicationUser user) =>
        (user.PreferredName ?? (user.FirstName + " " + user.LastName)).Trim();

    private static CommandValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
