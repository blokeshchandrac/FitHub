using FitHub.Domain.Identity;
namespace FitHub.Application.Recommendations;

public sealed record RecentWorkout(DateTimeOffset CompletedAt,IReadOnlyCollection<string> MuscleGroups);
public sealed record SuccessfulSet(Guid ExerciseId,string ExerciseName,decimal WeightKg,int Repetitions,DateTimeOffset CompletedAt);
public sealed record RecommendationContext(FitnessGoal Goal,ActivityLevel ActivityLevel,DateTimeOffset Now,DateTimeOffset? LastAttendanceAt,bool AttendedToday,bool HasAttendanceThisWeek,int PreviousAttendanceStreakWeeks,IReadOnlyCollection<RecentWorkout> RecentWorkouts,IReadOnlyCollection<SuccessfulSet> SuccessfulSets,decimal? LatestWeightKg);
public sealed record RecommendationDraft(string RuleCode,string Title,string Content,string Category,string Reason);

public static class RecommendationRules
{
    public const string NutritionDisclaimer="General guidance only; this is not medical advice. Consult a qualified professional for individual nutrition needs.";
    private static readonly string[] MajorMuscleGroups=["Chest","Back","Legs","Shoulders","Arms","Core"];

    public static IReadOnlyCollection<RecommendationDraft> Generate(RecommendationContext context)
    {
        var results=new List<RecommendationDraft>{TodayWorkout(context)};
        results.AddRange(TargetWeights(context));
        var missing=MissingMuscleGroups(context);if(missing is not null)results.Add(missing);
        var attendance=AttendanceReminder(context);if(attendance is not null)results.Add(attendance);
        var streak=StreakReminder(context);if(streak is not null)results.Add(streak);
        var nutrition=NutritionEstimate(context);if(nutrition is not null)results.Add(nutrition);
        return results;
    }

    public static RecommendationDraft TodayWorkout(RecommendationContext c)
    {
        if(c.RecentWorkouts.Any(x=>x.CompletedAt.UtcDateTime.Date==c.Now.UtcDateTime.Date))return new("TODAY_RECOVERY","Recovery is today's plan","You already completed a workout today. Choose mobility, walking or rest instead of another hard session.","Workout","A completed workout is recorded today.");
        var days=c.LastAttendanceAt is null?(double?)null:(c.Now-c.LastAttendanceAt.Value).TotalDays;
        if(days is null)return new("TODAY_COMEBACK","Today's workout: gentle full body","Start with a manageable full-body session and keep two or three repetitions in reserve.","Workout","No attendance has been recorded yet.");
        if(days>=3)return new("TODAY_COMEBACK","Today's workout: gentle full body","Start with a manageable full-body session and keep two or three repetitions in reserve.","Workout",$"The last gym attendance was {Math.Floor(days.Value)} days ago.");
        var focusMuscle=MajorMuscleGroups.OrderBy(group=>c.RecentWorkouts.Where(x=>x.MuscleGroups.Contains(group,StringComparer.OrdinalIgnoreCase)).Select(x=>x.CompletedAt).DefaultIfEmpty(DateTimeOffset.MinValue).Max()).First();
        var focus=c.Goal switch{FitnessGoal.Strength=>$"a compound strength session with {focusMuscle} emphasis",FitnessGoal.MuscleGain=>$"a hypertrophy session emphasizing {focusMuscle}",FitnessGoal.Endurance=>"a moderate conditioning session",FitnessGoal.WeightManagement=>"a full-body circuit plus comfortable cardio",_=>"a balanced full-body session"};
        return new("TODAY_GOAL","Today's workout suggestion",$"Choose {focus}.","Workout",$"Goal: {c.Goal}; recent workouts: {c.RecentWorkouts.Count}; least-recently trained major group: {focusMuscle}; attended today: {c.AttendedToday}.");
    }

    public static IReadOnlyCollection<RecommendationDraft> TargetWeights(RecommendationContext c)=>c.SuccessfulSets.GroupBy(x=>new{x.ExerciseId,x.ExerciseName}).Select(g=>g.OrderByDescending(x=>x.CompletedAt).ThenByDescending(x=>x.WeightKg).First()).OrderByDescending(x=>x.CompletedAt).Take(3).Select(x=>{var target=Math.Round(x.WeightKg*1.025m*2m,0,MidpointRounding.AwayFromZero)/2m;return new RecommendationDraft("TARGET_WEIGHT_PROGRESSIVE_OVERLOAD",$"Target weight: {x.ExerciseName}",$"Try {target:0.#} kg while maintaining safe technique and the planned repetitions.","Workout",$"The latest successful set was {x.WeightKg:0.#} kg for {x.Repetitions} repetitions; the rule adds 2.5% and rounds to 0.5 kg.");}).ToList();

    public static RecommendationDraft? MissingMuscleGroups(RecommendationContext c)
    {
        var cutoff=c.Now.AddDays(-7);var trained=c.RecentWorkouts.Where(x=>x.CompletedAt>=cutoff).SelectMany(x=>x.MuscleGroups).ToHashSet(StringComparer.OrdinalIgnoreCase);var missing=MajorMuscleGroups.Where(x=>!trained.Contains(x)).ToList();
        return missing.Count==0?null:new("MISSING_MUSCLE_GROUPS","Muscle groups to revisit",$"Consider including: {string.Join(", ",missing)}.","Workout",$"No completed exercise for these groups was found in the last 7 days: {string.Join(", ",missing)}.");
    }
    public static RecommendationDraft? AttendanceReminder(RecommendationContext c){if(c.LastAttendanceAt is not null&&(c.Now-c.LastAttendanceAt.Value).TotalDays<3)return null;return new("ATTENDANCE_LAPSE","Plan your next visit","Schedule a realistic gym visit to restart your routine.","Attendance",c.LastAttendanceAt is null?"No attendance has been recorded.":"There has been no attendance for at least 3 days.");}
    public static RecommendationDraft? StreakReminder(RecommendationContext c){if(c.HasAttendanceThisWeek||c.PreviousAttendanceStreakWeeks<2)return null;return new("ATTENDANCE_STREAK_AT_RISK","Keep your attendance streak","Record a visit this week if it fits your recovery and schedule.","Attendance",$"The previous attendance streak is {c.PreviousAttendanceStreakWeeks} weeks and no visit is recorded this week.");}
    public static RecommendationDraft? NutritionEstimate(RecommendationContext c)
    {
        if(c.LatestWeightKg is null or <=0)return null;var factor=c.ActivityLevel switch{ActivityLevel.Sedentary=>1.2m,ActivityLevel.Light=>1.35m,ActivityLevel.Moderate=>1.5m,_=>1.7m};var goalFactor=c.Goal switch{FitnessGoal.WeightManagement=>.9m,FitnessGoal.MuscleGain=>1.05m,_=>1m};var calories=Math.Round(c.LatestWeightKg.Value*22m*factor*goalFactor/10m,0,MidpointRounding.AwayFromZero)*10m;var protein=Math.Round(c.LatestWeightKg.Value*1.6m,0);var fat=Math.Round(c.LatestWeightKg.Value*.8m,0);var carbs=Math.Max(0m,Math.Round((calories-protein*4m-fat*9m)/4m,0));return new("NUTRITION_BASIC_ESTIMATE","General nutrition estimate",$"About {calories:0} kcal, {protein:0} g protein, {fat:0} g fat and {carbs:0} g carbohydrate per day. {NutritionDisclaimer}","Nutrition",$"Estimate uses latest weight {c.LatestWeightKg:0.#} kg, activity {c.ActivityLevel}, goal {c.Goal}, 22 kcal/kg baseline, and fixed macro factors.");
    }
}
