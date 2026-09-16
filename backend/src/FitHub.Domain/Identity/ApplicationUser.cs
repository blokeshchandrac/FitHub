using Microsoft.AspNetCore.Identity;

namespace FitHub.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? PreferredName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public bool OnboardingCompleted { get; private set; }
    public FitnessGoal FitnessGoal { get; private set; } = FitnessGoal.GeneralFitness;
    public ActivityLevel ActivityLevel { get; private set; } = ActivityLevel.Moderate;
    public ExperienceLevel? ExperienceLevel { get; private set; }
    public TrainingDaySchedule PlannedTrainingDays { get; private set; } = TrainingDaySchedule.None;
    public Guid? PreferredGymBranchId { get; private set; }
    public GymBranch? PreferredGymBranch { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public void MarkUpdated(DateTimeOffset timestamp) => UpdatedAt = timestamp;
    public void UpdateRecommendationPreferences(FitnessGoal goal, ActivityLevel activityLevel, DateTimeOffset timestamp){FitnessGoal=goal;ActivityLevel=activityLevel;MarkUpdated(timestamp);}

    public void UpdateProfile(
        string preferredName,
        DateOnly dateOfBirth,
        decimal heightCm,
        decimal weightKg,
        FitnessGoal fitnessGoal,
        ActivityLevel activityLevel,
        ExperienceLevel experienceLevel,
        IReadOnlyCollection<TrainingDay> plannedTrainingDays,
        Guid preferredGymBranchId,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(preferredName) || preferredName.Trim().Length > 50)
            throw new ArgumentException("Preferred name must contain between 1 and 50 characters.", nameof(preferredName));
        if (heightCm is < 80m or > 250m)
            throw new ArgumentOutOfRangeException(nameof(heightCm));
        if (weightKg is < 25m or > 350m)
            throw new ArgumentOutOfRangeException(nameof(weightKg));
        if (!Enum.IsDefined(experienceLevel))
            throw new ArgumentOutOfRangeException(nameof(experienceLevel));
        if (plannedTrainingDays.Count == 0)
            throw new ArgumentException("Select at least one planned training day.", nameof(plannedTrainingDays));
        if (plannedTrainingDays.Distinct().Count() != plannedTrainingDays.Count || plannedTrainingDays.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Planned training days must be valid and unique.", nameof(plannedTrainingDays));
        if (preferredGymBranchId == Guid.Empty)
            throw new ArgumentException("Preferred gym branch is required.", nameof(preferredGymBranchId));

        PreferredName = preferredName.Trim();
        DateOfBirth = dateOfBirth;
        HeightCm = decimal.Round(heightCm, 1);
        WeightKg = decimal.Round(weightKg, 1);
        FitnessGoal = fitnessGoal;
        ActivityLevel = activityLevel;
        ExperienceLevel = experienceLevel;
        PlannedTrainingDays = plannedTrainingDays.Aggregate(TrainingDaySchedule.None, (schedule, day) => schedule | ToSchedule(day));
        PreferredGymBranchId = preferredGymBranchId;
        OnboardingCompleted = true;
        MarkUpdated(timestamp);
    }

    public void MarkOnboardingCompleteForSeed(Guid branchId, DateTimeOffset timestamp)
    {
        PreferredName ??= FirstName;
        DateOfBirth ??= new DateOnly(1995, 1, 1);
        HeightCm ??= 175m;
        WeightKg ??= 75m;
        ExperienceLevel ??= FitHub.Domain.Identity.ExperienceLevel.Beginner;
        if (PlannedTrainingDays == TrainingDaySchedule.None)
            PlannedTrainingDays = TrainingDaySchedule.Monday | TrainingDaySchedule.Wednesday | TrainingDaySchedule.Friday;
        PreferredGymBranchId ??= branchId;
        OnboardingCompleted = true;
        MarkUpdated(timestamp);
    }

    public IReadOnlyCollection<TrainingDay> GetPlannedTrainingDays() =>
        Enum.GetValues<TrainingDay>().Where(day => PlannedTrainingDays.HasFlag(ToSchedule(day))).ToArray();

    private static TrainingDaySchedule ToSchedule(TrainingDay day) => day switch
    {
        TrainingDay.Monday => TrainingDaySchedule.Monday,
        TrainingDay.Tuesday => TrainingDaySchedule.Tuesday,
        TrainingDay.Wednesday => TrainingDaySchedule.Wednesday,
        TrainingDay.Thursday => TrainingDaySchedule.Thursday,
        TrainingDay.Friday => TrainingDaySchedule.Friday,
        TrainingDay.Saturday => TrainingDaySchedule.Saturday,
        TrainingDay.Sunday => TrainingDaySchedule.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day))
    };
}
