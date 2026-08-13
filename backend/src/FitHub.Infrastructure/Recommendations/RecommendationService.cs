using FitHub.Application.Analytics;using FitHub.Application.Recommendations;using FitHub.Domain.Communications;using FitHub.Domain.Identity;using FitHub.Domain.Workouts;using FitHub.Infrastructure.Persistence;using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.Recommendations;
public sealed class RecommendationService(FitHubDbContext db,TimeProvider timeProvider):IRecommendationService
{
    public async Task<RecommendationOverviewDto> GenerateAsync(Guid userId,CancellationToken ct=default)
    {
        var now=timeProvider.GetUtcNow();var user=await db.Users.SingleOrDefaultAsync(x=>x.Id==userId,ct)??throw new KeyNotFoundException("User not found.");
        var attendance=await db.Attendances.AsNoTracking().Where(x=>x.UserId==userId&&x.CheckedInAt>=now.AddDays(-90)).OrderByDescending(x=>x.CheckedInAt).Select(x=>x.CheckedInAt).ToListAsync(ct);
        var sessions=await db.WorkoutSessions.AsNoTracking().Include(x=>x.ExerciseLogs).ThenInclude(x=>x.Exercise).Include(x=>x.ExerciseLogs).ThenInclude(x=>x.Sets).Where(x=>x.UserId==userId&&x.Status==WorkoutSessionStatus.Completed&&x.EndedAt>=now.AddDays(-90)).OrderByDescending(x=>x.EndedAt).ToListAsync(ct);
        var recent=sessions.Where(x=>x.EndedAt>=now.AddDays(-14)).Select(x=>new RecentWorkout(x.EndedAt??x.StartedAt,x.ExerciseLogs.Select(e=>e.Exercise.MuscleGroup).Distinct(StringComparer.OrdinalIgnoreCase).ToList())).ToList();
        var sets=sessions.SelectMany(s=>s.ExerciseLogs.SelectMany(e=>e.Sets.Where(set=>set.WeightKg>0&&set.Repetitions>=8).Select(set=>new SuccessfulSet(e.ExerciseId,e.Exercise.Name,set.WeightKg!.Value,set.Repetitions!.Value,s.EndedAt??s.StartedAt)))).ToList();
        var weight=await db.BodyMeasurements.AsNoTracking().Where(x=>x.UserId==userId&&x.WeightKg>0).OrderByDescending(x=>x.RecordedAt).Select(x=>x.WeightKg).FirstOrDefaultAsync(ct);
        var weekStart=AnalyticsRules.StartOfUtcWeek(now);var context=new RecommendationContext(user.FitnessGoal,user.ActivityLevel,now,attendance.Count==0?null:attendance[0],attendance.Any(x=>x.UtcDateTime.Date==now.UtcDateTime.Date),attendance.Any(x=>x>=weekStart),AnalyticsRules.CurrentWeeklyStreak(attendance,now.AddDays(-7)),recent,sets,weight);
        var existing=await db.Recommendations.Where(x=>x.UserId==userId&&x.Status==RecommendationStatus.Active).ToListAsync(ct);foreach(var item in existing)item.Expire(now);
        var generated=RecommendationRules.Generate(context).Select(x=>new Recommendation(userId,x.RuleCode,x.Title,x.Content,x.Category,x.Reason,now.AddDays(1))).ToList();db.Recommendations.AddRange(generated);await db.SaveChangesAsync(ct);return Overview(user,generated);
    }
    public async Task<RecommendationOverviewDto> UpdatePreferencesAsync(Guid userId,UpdateRecommendationPreferencesCommand command,CancellationToken ct=default)
    {if(!Enum.TryParse<FitnessGoal>(command.FitnessGoal,true,out var goal)||!Enum.TryParse<ActivityLevel>(command.ActivityLevel,true,out var activity))throw new ArgumentException("Unknown fitness goal or activity level.");var user=await db.Users.SingleOrDefaultAsync(x=>x.Id==userId,ct)??throw new KeyNotFoundException("User not found.");user.UpdateRecommendationPreferences(goal,activity,timeProvider.GetUtcNow());await db.SaveChangesAsync(ct);return await GenerateAsync(userId,ct);}
    private static RecommendationOverviewDto Overview(ApplicationUser user,IEnumerable<Recommendation> items)=>new(user.FitnessGoal.ToString(),user.ActivityLevel.ToString(),RecommendationRules.NutritionDisclaimer,items.Select(x=>new RecommendationDto(x.Id,x.RuleCode,x.Title,x.Content,x.Category,x.Reason,x.CreatedAt)).ToList());
}
