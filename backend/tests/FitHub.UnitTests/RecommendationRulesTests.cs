using FitHub.Application.Recommendations;using FitHub.Domain.Identity;
namespace FitHub.UnitTests;
public sealed class RecommendationRulesTests
{
    private static readonly DateTimeOffset Now=new(2026,8,12,12,0,0,TimeSpan.Zero);
    [Fact]public void TodayRuleChoosesRecoveryAfterWorkout(){var c=Context() with{RecentWorkouts=[new(Now,["Legs"])]};Assert.Equal("TODAY_RECOVERY",RecommendationRules.TodayWorkout(c).RuleCode);}
    [Fact]public void TodayRuleUsesAttendanceLapse(){var c=Context() with{LastAttendanceAt=Now.AddDays(-4)};Assert.Equal("TODAY_COMEBACK",RecommendationRules.TodayWorkout(c).RuleCode);}
    [Theory][InlineData(FitnessGoal.Strength,"strength")][InlineData(FitnessGoal.MuscleGain,"hypertrophy")][InlineData(FitnessGoal.Endurance,"conditioning")][InlineData(FitnessGoal.WeightManagement,"circuit")][InlineData(FitnessGoal.GeneralFitness,"balanced")]public void TodayRuleUsesGoal(FitnessGoal goal,string expected){var result=RecommendationRules.TodayWorkout(Context() with{Goal=goal});Assert.Equal("TODAY_GOAL",result.RuleCode);Assert.Contains(expected,result.Content,StringComparison.OrdinalIgnoreCase);}
    [Fact]public void TargetWeightUsesSuccessfulPreviousSet(){var set=new SuccessfulSet(Guid.NewGuid(),"Bench press",100m,8,Now.AddDays(-1));var result=Assert.Single(RecommendationRules.TargetWeights(Context() with{SuccessfulSets=[set]}));Assert.Contains("102.5",result.Content);Assert.Contains("2.5%",result.Reason);}
    [Fact]public void TargetWeightRuleProducesNothingWithoutSuccessfulSets()=>Assert.Empty(RecommendationRules.TargetWeights(Context()));
    [Fact]public void MissingMuscleRuleNamesUntrainedGroups(){var result=RecommendationRules.MissingMuscleGroups(Context() with{RecentWorkouts=[new(Now.AddDays(-1),["Chest"])]});Assert.NotNull(result);Assert.Contains("Back",result!.Content);Assert.DoesNotContain("Chest,",result.Content);}
    [Fact]public void MissingMuscleRuleStopsWhenAllGroupsWereTrained(){var result=RecommendationRules.MissingMuscleGroups(Context() with{RecentWorkouts=[new(Now.AddDays(-1),["Chest","Back","Legs","Shoulders","Arms","Core"])]});Assert.Null(result);}
    [Fact]public void AttendanceReminderAppearsAfterThreeDays(){Assert.Equal("ATTENDANCE_LAPSE",RecommendationRules.AttendanceReminder(Context() with{LastAttendanceAt=Now.AddDays(-3)})?.RuleCode);Assert.Null(RecommendationRules.AttendanceReminder(Context() with{LastAttendanceAt=Now.AddDays(-1)}));}
    [Fact]public void StreakReminderAppearsOnlyWhenStreakIsAtRisk(){Assert.Equal("ATTENDANCE_STREAK_AT_RISK",RecommendationRules.StreakReminder(Context() with{HasAttendanceThisWeek=false,PreviousAttendanceStreakWeeks=3})?.RuleCode);Assert.Null(RecommendationRules.StreakReminder(Context() with{HasAttendanceThisWeek=true,PreviousAttendanceStreakWeeks=3}));}
    [Fact]public void NutritionRuleCalculatesMacrosAndIncludesDisclaimer(){var result=RecommendationRules.NutritionEstimate(Context() with{LatestWeightKg=80m});Assert.NotNull(result);Assert.Contains("protein",result!.Content);Assert.Contains("not medical advice",result.Content);Assert.Contains("22 kcal/kg",result.Reason);}
    [Fact]public void NutritionRuleRequiresWeight()=>Assert.Null(RecommendationRules.NutritionEstimate(Context()));
    [Fact]public void EveryGeneratedRecommendationStoresRuleAndReason(){var results=RecommendationRules.Generate(Context() with{LatestWeightKg=80m});Assert.All(results,x=>{Assert.False(string.IsNullOrWhiteSpace(x.RuleCode));Assert.False(string.IsNullOrWhiteSpace(x.Reason));});}
    private static RecommendationContext Context()=>new(FitnessGoal.GeneralFitness,ActivityLevel.Moderate,Now,Now.AddDays(-1),false,true,0,[],[],null);
}
