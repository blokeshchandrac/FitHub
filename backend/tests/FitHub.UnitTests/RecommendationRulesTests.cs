using FitHub.Application.Recommendations;
using FitHub.Domain.Identity;

namespace FitHub.UnitTests;

public sealed class RecommendationRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact] public void TodayRuleChoosesRecoveryAfterWorkout() => Assert.Equal("TODAY_RECOVERY", RecommendationRules.TodayWorkout(Context() with { RecentWorkouts = [new(Now, ["Legs"])] }).RuleCode);
    [Fact] public void TodayRuleUsesAttendanceLapse() => Assert.Equal("TODAY_COMEBACK", RecommendationRules.TodayWorkout(Context() with { LastAttendanceAt = Now.AddDays(-4) }).RuleCode);

    [Theory]
    [InlineData(FitnessGoal.Strength, "strength")]
    [InlineData(FitnessGoal.MuscleGain, "hypertrophy")]
    [InlineData(FitnessGoal.Endurance, "conditioning")]
    [InlineData(FitnessGoal.WeightManagement, "circuit")]
    [InlineData(FitnessGoal.GeneralFitness, "balanced")]
    public void TodayRuleUsesGoal(FitnessGoal goal, string expected)
    {
        var result = RecommendationRules.TodayWorkout(Context() with { Goal = goal });
        Assert.Equal("TODAY_GOAL", result.RuleCode);
        Assert.Contains(expected, result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ExperienceLevel.Beginner, "102.5")]
    [InlineData(ExperienceLevel.Intermediate, "102")]
    [InlineData(ExperienceLevel.Advanced, "101")]
    public void TargetWeightUsesExperienceAwareLimit(ExperienceLevel experience, string target)
    {
        var set = new SuccessfulSet(Guid.NewGuid(), "Bench press", 100m, 8, Now.AddDays(-1));
        var result = Assert.Single(RecommendationRules.TargetWeights(Context() with { ExperienceLevel = experience, SuccessfulSets = [set] }));
        Assert.Contains(target, result.Content);
        Assert.Contains(experience.ToString(), result.Reason);
    }

    [Fact] public void TargetWeightRuleProducesNothingWithoutSuccessfulSets() => Assert.Empty(RecommendationRules.TargetWeights(Context()));

    [Fact]
    public void MissedPlannedDayUsesMemberSchedule()
    {
        var result = RecommendationRules.MissedPlannedDay(Context() with
        {
            PlannedTrainingDays = [TrainingDay.Monday, TrainingDay.Wednesday],
            AttendanceHistory = [Now.AddDays(-1)],
            RecentWorkouts = [new(Now.AddDays(-1), ["Legs"])]
        });
        Assert.NotNull(result);
        Assert.Equal("MISSED_PLANNED_DAY", result!.RuleCode);
        Assert.Contains("Monday", result.Content);
    }

    [Fact]
    public void ConsistencyComparesActivityWithPlannedDays()
    {
        var result = RecommendationRules.TrainingConsistency(Context() with
        {
            PlannedTrainingDays = [TrainingDay.Monday],
            AttendanceHistory = [new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero)]
        });
        Assert.Equal("PLANNED_DAY_CONSISTENCY", result.RuleCode);
        Assert.Contains("1 of 4 planned days", result.Content);
    }

    [Fact]
    public void MissingInformationReturnsHonestNotEnoughDataResult()
    {
        var result = Assert.Single(RecommendationRules.Generate(Context() with
        {
            ProfileComplete = false,
            PlannedTrainingDays = [],
            AttendanceHistory = [],
            HasCompletedWorkoutHistory = false,
            RecentWorkouts = []
        }));
        Assert.Equal("INSUFFICIENT_DATA", result.RuleCode);
        Assert.Equal("Not enough data", result.Title);
        Assert.Contains("Incomplete profile", result.Reason);
        Assert.Contains("No completed workout", result.Reason);
    }

    [Fact]
    public void MissingMuscleRuleNamesUntrainedGroups()
    {
        var result = RecommendationRules.MissingMuscleGroups(Context() with { RecentWorkouts = [new(Now.AddDays(-1), ["Chest"])] });
        Assert.NotNull(result);
        Assert.Contains("Back", result!.Content);
        Assert.DoesNotContain("Chest,", result.Content);
    }

    [Fact] public void MissingMuscleRuleStopsWhenAllGroupsWereTrained() => Assert.Null(RecommendationRules.MissingMuscleGroups(Context() with { RecentWorkouts = [new(Now.AddDays(-1), ["Chest", "Back", "Legs", "Shoulders", "Arms", "Core"])] }));
    [Fact] public void AttendanceReminderAppearsAfterThreeDays() { Assert.Equal("ATTENDANCE_LAPSE", RecommendationRules.AttendanceReminder(Context() with { LastAttendanceAt = Now.AddDays(-3) })?.RuleCode); Assert.Null(RecommendationRules.AttendanceReminder(Context() with { LastAttendanceAt = Now.AddDays(-1) })); }
    [Fact] public void StreakReminderAppearsOnlyWhenStreakIsAtRisk() { Assert.Equal("ATTENDANCE_STREAK_AT_RISK", RecommendationRules.StreakReminder(Context() with { HasAttendanceThisWeek = false, PreviousAttendanceStreakWeeks = 3 })?.RuleCode); Assert.Null(RecommendationRules.StreakReminder(Context() with { HasAttendanceThisWeek = true, PreviousAttendanceStreakWeeks = 3 })); }

    [Fact]
    public void NutritionRuleCalculatesMacrosAndIncludesDisclaimer()
    {
        var result = RecommendationRules.NutritionEstimate(Context() with { LatestWeightKg = 80m });
        Assert.NotNull(result);
        Assert.Contains("protein", result!.Content);
        Assert.Contains("not medical advice", result.Content);
        Assert.Contains("22 kcal/kg", result.Reason);
    }

    [Fact] public void NutritionRuleRequiresWeight() => Assert.Null(RecommendationRules.NutritionEstimate(Context()));

    [Fact]
    public void EveryGeneratedRecommendationStoresRuleAndReason()
    {
        var results = RecommendationRules.Generate(Context() with { LatestWeightKg = 80m });
        Assert.All(results, result => { Assert.False(string.IsNullOrWhiteSpace(result.RuleCode)); Assert.False(string.IsNullOrWhiteSpace(result.Reason)); });
    }

    private static RecommendationContext Context() => new(
        FitnessGoal.GeneralFitness,
        ActivityLevel.Moderate,
        ExperienceLevel.Beginner,
        [TrainingDay.Monday, TrainingDay.Wednesday, TrainingDay.Friday],
        true,
        Now,
        Now.AddDays(-1),
        false,
        true,
        0,
        [Now.AddDays(-1)],
        [new RecentWorkout(Now.AddDays(-1), ["Legs"])],
        true,
        [],
        null);
}
