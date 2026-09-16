using FitHub.Application.Profiles;

namespace FitHub.Application.Trainers;

public interface ITrainerMemberAssignmentService
{
    Task<IReadOnlyCollection<TrainerMemberAssignmentDto>> GetAssignmentsAsync(Guid trainerId, bool includeEnded, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EligibleMemberDto>> SearchEligibleMembersAsync(Guid trainerId, string? search, CancellationToken cancellationToken = default);
    Task<TrainerMemberAssignmentDto> AssignAsync(Guid administratorId, Guid trainerId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> UnassignAsync(Guid administratorId, Guid trainerId, Guid assignmentId, CancellationToken cancellationToken = default);
}

public interface ITrainerMemberAccessService
{
    Task<bool> IsAssignedAsync(Guid trainerId, Guid memberId, CancellationToken cancellationToken = default);
    Task EnsureAssignedAsync(Guid trainerId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AssignedMemberDto>> GetAssignedMembersAsync(Guid trainerId, CancellationToken cancellationToken = default);
}

public sealed record TrainerMemberAssignmentDto(
    Guid Id,
    Guid TrainerId,
    string TrainerName,
    Guid MemberId,
    string MemberName,
    string MemberEmail,
    string Status,
    DateTimeOffset AssignedAt,
    Guid AssignedByAdministratorId,
    string AssignedByAdministratorName,
    DateTimeOffset? UnassignedAt,
    Guid? UnassignedByAdministratorId,
    string? UnassignedByAdministratorName);

public sealed record EligibleMemberDto(Guid Id, string DisplayName, string Email);

public sealed record AssignedMemberDto(
    Guid Id,
    string DisplayName,
    string Email,
    DateTimeOffset AssignedAt,
    MemberProfileDto Profile);
