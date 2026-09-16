using FitHub.Domain.Identity;

namespace FitHub.Application.Profiles;

public interface IProfileService
{
    Task<MemberProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MemberProfileDto?> UpdateAsync(Guid userId, UpdateMemberProfileCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GymBranchDto>> GetActiveBranchesAsync(CancellationToken cancellationToken = default);
}

public sealed record GymBranchDto(Guid Id, string Code, string Name);

public sealed record MemberProfileDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    decimal? WeightKg,
    FitnessGoal FitnessGoal,
    ActivityLevel ActivityLevel,
    ExperienceLevel? ExperienceLevel,
    IReadOnlyCollection<TrainingDay> PlannedTrainingDays,
    Guid? PreferredGymBranchId,
    string? PreferredGymBranchName,
    bool OnboardingCompleted);

public sealed record UpdateMemberProfileCommand(
    string PreferredName,
    DateOnly DateOfBirth,
    decimal HeightCm,
    decimal WeightKg,
    FitnessGoal FitnessGoal,
    ActivityLevel ActivityLevel,
    ExperienceLevel ExperienceLevel,
    IReadOnlyCollection<TrainingDay> PlannedTrainingDays,
    Guid PreferredGymBranchId);
