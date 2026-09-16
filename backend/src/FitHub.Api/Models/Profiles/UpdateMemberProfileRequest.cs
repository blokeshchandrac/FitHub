using System.ComponentModel.DataAnnotations;
using FitHub.Domain.Identity;

namespace FitHub.Api.Models.Profiles;

public sealed record UpdateMemberProfileRequest(
    [Required, StringLength(50, MinimumLength = 1)] string PreferredName,
    DateOnly DateOfBirth,
    [Range(80, 250)] decimal HeightCm,
    [Range(25, 350)] decimal WeightKg,
    FitnessGoal FitnessGoal,
    ActivityLevel ActivityLevel,
    ExperienceLevel ExperienceLevel,
    [MinLength(1)] IReadOnlyCollection<TrainingDay> PlannedTrainingDays,
    Guid PreferredGymBranchId);
