using FitHub.Application.Analytics;
using FitHub.Application.Recommendations;
using FitHub.Domain.Communications;
using FitHub.Domain.Identity;
using FitHub.Domain.Workouts;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Recommendations;

public sealed class RecommendationService(FitHubDbContext db, TimeProvider timeProvider) : IRecommendationService
{
    public async Task<RecommendationOverviewDto> GenerateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var user = await db.Users.Include(item => item.PreferredGymBranch)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        var attendance = await db.Attendances.AsNoTracking()
            .Where(item => item.UserId == userId && item.CheckedInAt >= now.AddDays(-90))
            .OrderByDescending(item => item.CheckedInAt).Select(item => item.CheckedInAt).ToListAsync(cancellationToken);
        var sessions = await db.WorkoutSessions.AsNoTracking()
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Exercise)
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .Where(item => item.UserId == userId && item.Status == WorkoutSessionStatus.Completed && item.EndedAt >= now.AddDays(-90))
            .OrderByDescending(item => item.EndedAt).ToListAsync(cancellationToken);
        var recent = sessions.Where(item => item.EndedAt >= now.AddDays(-28)).Select(item =>
            new RecentWorkout(item.EndedAt ?? item.StartedAt, item.ExerciseLogs.Select(log => log.Exercise.MuscleGroup)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList())).ToList();
        var sets = sessions.SelectMany(session => session.ExerciseLogs.SelectMany(log => log.Sets
            .Where(set => set.WeightKg > 0 && set.Repetitions >= 8)
            .Select(set => new SuccessfulSet(log.ExerciseId, log.Exercise.Name, set.WeightKg!.Value,
                set.Repetitions!.Value, session.EndedAt ?? session.StartedAt)))).ToList();
        var weight = await db.BodyMeasurements.AsNoTracking().Where(item => item.UserId == userId && item.WeightKg > 0)
            .OrderByDescending(item => item.RecordedAt).Select(item => item.WeightKg).FirstOrDefaultAsync(cancellationToken);
        var weekStart = AnalyticsRules.StartOfUtcWeek(now);
        var profileComplete = user.OnboardingCompleted && user.ExperienceLevel is not null
            && user.PlannedTrainingDays != TrainingDaySchedule.None && user.PreferredGymBranch?.IsActive == true;
        var context = new RecommendationContext(
            user.FitnessGoal,
            user.ActivityLevel,
            user.ExperienceLevel,
            user.GetPlannedTrainingDays(),
            profileComplete,
            now,
            attendance.Count == 0 ? null : attendance[0],
            attendance.Any(item => item.UtcDateTime.Date == now.UtcDateTime.Date),
            attendance.Any(item => item >= weekStart),
            AnalyticsRules.CurrentWeeklyStreak(attendance, now.AddDays(-7)),
            attendance,
            recent,
            sessions.Count > 0,
            sets,
            weight == 0m ? null : weight);

        var existing = await db.Recommendations.Where(item => item.UserId == userId && item.Status == RecommendationStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var item in existing) item.Expire(now);
        var generated = RecommendationRules.Generate(context)
            .Select(item => new Recommendation(userId, item.RuleCode, item.Title, item.Content, item.Category, item.Reason, now.AddDays(1)))
            .ToList();
        db.Recommendations.AddRange(generated);
        await db.SaveChangesAsync(cancellationToken);
        return Overview(user, generated);
    }

    public async Task<RecommendationOverviewDto> UpdatePreferencesAsync(
        Guid userId, UpdateRecommendationPreferencesCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<FitnessGoal>(command.FitnessGoal, true, out var goal)
            || !Enum.TryParse<ActivityLevel>(command.ActivityLevel, true, out var activity))
            throw new ArgumentException("Unknown fitness goal or activity level.");
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        user.UpdateRecommendationPreferences(goal, activity, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return await GenerateAsync(userId, cancellationToken);
    }

    private static RecommendationOverviewDto Overview(ApplicationUser user, IEnumerable<Recommendation> items) =>
        new(user.FitnessGoal.ToString(), user.ActivityLevel.ToString(), RecommendationRules.NutritionDisclaimer,
            items.Select(item => new RecommendationDto(item.Id, item.RuleCode, item.Title, item.Content,
                item.Category, item.Reason, item.CreatedAt)).ToList());
}
