using FitHub.Domain.Identity;

namespace FitHub.Application.Recommendations;

public sealed record RecentWorkout(DateTimeOffset CompletedAt, IReadOnlyCollection<string> MuscleGroups);
public sealed record SuccessfulSet(Guid ExerciseId, string ExerciseName, decimal WeightKg, int Repetitions, DateTimeOffset CompletedAt);
public sealed record RecommendationContext(
    FitnessGoal Goal,
    ActivityLevel ActivityLevel,
    ExperienceLevel? ExperienceLevel,
    IReadOnlyCollection<TrainingDay> PlannedTrainingDays,
    bool ProfileComplete,
    DateTimeOffset Now,
    DateTimeOffset? LastAttendanceAt,
    bool AttendedToday,
    bool HasAttendanceThisWeek,
    int PreviousAttendanceStreakWeeks,
    IReadOnlyCollection<DateTimeOffset> AttendanceHistory,
    IReadOnlyCollection<RecentWorkout> RecentWorkouts,
    bool HasCompletedWorkoutHistory,
    IReadOnlyCollection<SuccessfulSet> SuccessfulSets,
    decimal? LatestWeightKg);
public sealed record RecommendationDraft(string RuleCode, string Title, string Content, string Category, string Reason);

public static class RecommendationRules
{
    public const string NutritionDisclaimer = "General guidance only; this is not medical advice. Consult a qualified professional for individual nutrition needs.";
    private static readonly string[] MajorMuscleGroups = ["Chest", "Back", "Legs", "Shoulders", "Arms", "Core"];

    public static IReadOnlyCollection<RecommendationDraft> Generate(RecommendationContext context)
    {
        var insufficient = InsufficientInformation(context);
        if (insufficient is not null) return [insufficient];

        var results = new List<RecommendationDraft> { TodayWorkout(context), TrainingConsistency(context) };
        results.AddRange(TargetWeights(context));
        var missedDay = MissedPlannedDay(context); if (missedDay is not null) results.Add(missedDay);
        var missing = MissingMuscleGroups(context); if (missing is not null) results.Add(missing);
        var attendance = AttendanceReminder(context); if (attendance is not null) results.Add(attendance);
        var streak = StreakReminder(context); if (streak is not null) results.Add(streak);
        var nutrition = NutritionEstimate(context); if (nutrition is not null) results.Add(nutrition);
        return results;
    }

    public static RecommendationDraft? InsufficientInformation(RecommendationContext context)
    {
        var missing = new List<string>();
        if (!context.ProfileComplete) missing.Add("Incomplete profile");
        if (context.PlannedTrainingDays.Count == 0) missing.Add("No planned training days");
        if (context.AttendanceHistory.Count == 0) missing.Add("No attendance history");
        if (!context.HasCompletedWorkoutHistory) missing.Add("No completed workout");
        return missing.Count == 0 ? null : new RecommendationDraft(
            "INSUFFICIENT_DATA", "Not enough data",
            $"Complete the missing information before FitHub creates a personalised training recommendation: {string.Join("; ", missing)}.",
            "Information", $"Recommendation generation paused because stored data is missing: {string.Join("; ", missing)}.");
    }

    public static RecommendationDraft TodayWorkout(RecommendationContext context)
    {
        if (context.RecentWorkouts.Any(workout => workout.CompletedAt.UtcDateTime.Date == context.Now.UtcDateTime.Date))
            return new("TODAY_RECOVERY", "Recovery is today's plan", "You already completed a workout today. Choose mobility, walking or rest instead of another hard session.", "Workout", "A completed workout is recorded today.");
        var days = context.LastAttendanceAt is null ? (double?)null : (context.Now - context.LastAttendanceAt.Value).TotalDays;
        if (days is null || days >= 3)
            return new("TODAY_COMEBACK", "Today's workout: gentle full body", "Start with a manageable full-body session and keep two or three repetitions in reserve.", "Workout",
                days is null ? "No attendance has been recorded yet." : $"The last gym attendance was {Math.Floor(days.Value)} days ago.");
        var focusMuscle = MajorMuscleGroups.OrderBy(group => context.RecentWorkouts
            .Where(workout => workout.MuscleGroups.Contains(group, StringComparer.OrdinalIgnoreCase))
            .Select(workout => workout.CompletedAt).DefaultIfEmpty(DateTimeOffset.MinValue).Max()).First();
        var focus = context.Goal switch
        {
            FitnessGoal.Strength => $"a compound strength session with {focusMuscle} emphasis",
            FitnessGoal.MuscleGain => $"a hypertrophy session emphasizing {focusMuscle}",
            FitnessGoal.Endurance => "a moderate conditioning session",
            FitnessGoal.WeightManagement => "a full-body circuit plus comfortable cardio",
            _ => "a balanced full-body session"
        };
        return new("TODAY_GOAL", "Today's workout suggestion", $"Choose {focus} at a level appropriate for {context.ExperienceLevel} experience.", "Workout",
            $"Goal: {context.Goal}; experience: {context.ExperienceLevel}; recent workouts: {context.RecentWorkouts.Count}; least-recently trained major group: {focusMuscle}; attended today: {context.AttendedToday}.");
    }

    public static IReadOnlyCollection<RecommendationDraft> TargetWeights(RecommendationContext context)
    {
        var increase = context.ExperienceLevel switch
        {
            ExperienceLevel.Advanced => 0.01m,
            ExperienceLevel.Intermediate => 0.02m,
            _ => 0.025m
        };
        return context.SuccessfulSets.GroupBy(set => new { set.ExerciseId, set.ExerciseName })
            .Select(group => group.OrderByDescending(set => set.CompletedAt).ThenByDescending(set => set.WeightKg).First())
            .OrderByDescending(set => set.CompletedAt).Take(3).Select(set =>
            {
                var target = Math.Round(set.WeightKg * (1m + increase) * 2m, 0, MidpointRounding.AwayFromZero) / 2m;
                return new RecommendationDraft("TARGET_WEIGHT_PROGRESSIVE_OVERLOAD", $"Target weight: {set.ExerciseName}",
                    $"Try {target:0.#} kg while maintaining safe technique and the planned repetitions.", "Workout",
                    $"The latest successful set was {set.WeightKg:0.#} kg for {set.Repetitions} repetitions; the {context.ExperienceLevel} rule limits the increase to {increase:P1} and rounds to 0.5 kg.");
            }).ToList();
    }

    public static RecommendationDraft? MissedPlannedDay(RecommendationContext context)
    {
        var planned = context.PlannedTrainingDays.ToHashSet();
        var missedDate = Enumerable.Range(1, 7).Select(offset => context.Now.AddDays(-offset))
            .FirstOrDefault(date => planned.Contains(ToTrainingDay(date.DayOfWeek))
                && !HasActivityOn(date, context.AttendanceHistory, context.RecentWorkouts));
        if (missedDate == default) return null;
        return new("MISSED_PLANNED_DAY", "A planned training day was missed",
            $"Your {missedDate:dddd} session has no recorded attendance or completed workout. Choose the next suitable planned day rather than trying to make up everything at once.",
            "Consistency", $"{missedDate:yyyy-MM-dd} is in the stored training-day schedule, with no qualifying attendance or completed workout.");
    }

    public static RecommendationDraft TrainingConsistency(RecommendationContext context)
    {
        var planned = context.PlannedTrainingDays.ToHashSet();
        var dates = Enumerable.Range(0, 28).Select(offset => context.Now.AddDays(-offset))
            .Where(date => planned.Contains(ToTrainingDay(date.DayOfWeek))).ToList();
        var completed = dates.Count(date => HasActivityOn(date, context.AttendanceHistory, context.RecentWorkouts));
        var percent = dates.Count == 0 ? 0m : decimal.Round(completed * 100m / dates.Count, 0);
        return new("PLANNED_DAY_CONSISTENCY", "Training consistency", $"You completed activity on {completed} of {dates.Count} planned days ({percent:0}%) in the last 28 days.",
            "Consistency", $"Stored planned weekdays were compared with distinct attendance or completed-workout dates; current weekly attendance streak: {context.PreviousAttendanceStreakWeeks} week(s).");
    }

    public static RecommendationDraft? MissingMuscleGroups(RecommendationContext context)
    {
        var cutoff = context.Now.AddDays(-7);
        var trained = context.RecentWorkouts.Where(workout => workout.CompletedAt >= cutoff)
            .SelectMany(workout => workout.MuscleGroups).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = MajorMuscleGroups.Where(group => !trained.Contains(group)).ToList();
        return missing.Count == 0 ? null : new("MISSING_MUSCLE_GROUPS", "Muscle groups to revisit", $"Consider including: {string.Join(", ", missing)}.", "Workout",
            $"No completed exercise for these groups was found in the last 7 days: {string.Join(", ", missing)}.");
    }

    public static RecommendationDraft? AttendanceReminder(RecommendationContext context)
    {
        if (context.LastAttendanceAt is not null && (context.Now - context.LastAttendanceAt.Value).TotalDays < 3) return null;
        return new("ATTENDANCE_LAPSE", "Plan your next visit", "Schedule a realistic gym visit to restart your routine.", "Attendance",
            context.LastAttendanceAt is null ? "No attendance has been recorded." : "There has been no attendance for at least 3 days.");
    }

    public static RecommendationDraft? StreakReminder(RecommendationContext context)
    {
        if (context.HasAttendanceThisWeek || context.PreviousAttendanceStreakWeeks < 2) return null;
        return new("ATTENDANCE_STREAK_AT_RISK", "Keep your attendance streak", "Record a visit this week if it fits your recovery and schedule.", "Attendance",
            $"The previous attendance streak is {context.PreviousAttendanceStreakWeeks} weeks and no visit is recorded this week.");
    }

    public static RecommendationDraft? NutritionEstimate(RecommendationContext context)
    {
        if (context.LatestWeightKg is null or <= 0) return null;
        var factor = context.ActivityLevel switch { ActivityLevel.Sedentary => 1.2m, ActivityLevel.Light => 1.35m, ActivityLevel.Moderate => 1.5m, _ => 1.7m };
        var goalFactor = context.Goal switch { FitnessGoal.WeightManagement => .9m, FitnessGoal.MuscleGain => 1.05m, _ => 1m };
        var calories = Math.Round(context.LatestWeightKg.Value * 22m * factor * goalFactor / 10m, 0, MidpointRounding.AwayFromZero) * 10m;
        var protein = Math.Round(context.LatestWeightKg.Value * 1.6m, 0);
        var fat = Math.Round(context.LatestWeightKg.Value * .8m, 0);
        var carbs = Math.Max(0m, Math.Round((calories - protein * 4m - fat * 9m) / 4m, 0));
        return new("NUTRITION_BASIC_ESTIMATE", "General nutrition estimate", $"About {calories:0} kcal, {protein:0} g protein, {fat:0} g fat and {carbs:0} g carbohydrate per day. {NutritionDisclaimer}", "Nutrition",
            $"Estimate uses latest weight {context.LatestWeightKg:0.#} kg, activity {context.ActivityLevel}, goal {context.Goal}, 22 kcal/kg baseline, and fixed macro factors.");
    }

    private static bool HasActivityOn(DateTimeOffset date, IReadOnlyCollection<DateTimeOffset> attendance, IReadOnlyCollection<RecentWorkout> workouts) =>
        attendance.Any(item => item.UtcDateTime.Date == date.UtcDateTime.Date)
        || workouts.Any(item => item.CompletedAt.UtcDateTime.Date == date.UtcDateTime.Date);

    private static TrainingDay ToTrainingDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => TrainingDay.Monday,
        DayOfWeek.Tuesday => TrainingDay.Tuesday,
        DayOfWeek.Wednesday => TrainingDay.Wednesday,
        DayOfWeek.Thursday => TrainingDay.Thursday,
        DayOfWeek.Friday => TrainingDay.Friday,
        DayOfWeek.Saturday => TrainingDay.Saturday,
        _ => TrainingDay.Sunday
    };
}
