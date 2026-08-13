using FitHub.Application.Analytics;
using FitHub.Application.Attendance;
using FitHub.Application.Dashboard;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;
using FitHub.Domain.Workouts;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Dashboard;

internal sealed class DashboardService(FitHubDbContext dbContext, TimeProvider timeProvider) : IDashboardService
{
    public async Task<MemberDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var fourWeeksAgo = now.AddDays(-28);
        var weekStart = AnalyticsRules.StartOfUtcWeek(now);
        var attendanceCount = await dbContext.Attendances.AsNoTracking().CountAsync(item => item.UserId == userId && item.CheckedInAt >= fourWeeksAgo, cancellationToken);
        var completedSessions = await dbContext.WorkoutSessions.AsNoTracking()
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Exercise)
            .Include(item => item.ExerciseLogs).ThenInclude(item => item.Sets)
            .Where(item => item.UserId == userId && item.Status == WorkoutSessionStatus.Completed)
            .OrderByDescending(item => item.EndedAt).ToListAsync(cancellationToken);
        var workoutCount = completedSessions.Count(item => item.EndedAt >= fourWeeksAgo);
        var weeklyVolume = completedSessions.Where(item => item.EndedAt >= weekStart)
            .Sum(item => AnalyticsRules.WorkoutVolume(item.ExerciseLogs.SelectMany(log => log.Sets).Select(set => (set.Repetitions, set.WeightKg))));
        var completedAt = completedSessions.Select(item => item.EndedAt ?? item.StartedAt).ToList();
        var records = PersonalRecords(completedSessions);

        var weights = await dbContext.BodyMeasurements.AsNoTracking()
            .Where(item => item.UserId == userId && item.WeightKg != null)
            .OrderByDescending(item => item.RecordedAt).Take(180).ToListAsync(cancellationToken);
        weights.Reverse();
        var weightPoints = weights.Select(item => new WeightTrendPointDto(item.RecordedAt, item.WeightKg!.Value)).ToList();
        var latestWeight = weightPoints.LastOrDefault()?.WeightKg;
        decimal? weightChange = weightPoints.Count < 2 ? null : decimal.Round(weightPoints[^1].WeightKg - weightPoints[0].WeightKg, 2);

        var membership = await dbContext.Memberships.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.EndsAt).FirstOrDefaultAsync(cancellationToken);
        var membershipVisits = membership is null ? 0 : await dbContext.Attendances.AsNoTracking().CountAsync(
            item => item.UserId == userId && item.CheckedInAt >= membership.StartsAt && item.CheckedInAt < membership.EndsAt, cancellationToken);
        var membershipCost = membership is null ? 0m : await dbContext.Payments.AsNoTracking()
            .Where(item => item.MembershipId == membership.Id && item.Status == PaymentStatus.Succeeded)
            .SumAsync(item => item.Amount, cancellationToken);

        return new MemberDashboardDto(
            decimal.Round(attendanceCount / 4m, 1),
            decimal.Round(workoutCount / 4m, 1),
            weeklyVolume,
            records,
            AnalyticsRules.CurrentWeeklyStreak(completedAt, now),
            AnalyticsRules.ConsistencyScore(completedAt, now),
            new WeightTrendDto(latestWeight, weightChange, weightPoints),
            membershipVisits,
            membershipCost > 0 ? MembershipCostCalculator.Calculate(membershipCost, membershipVisits) : null);
    }

    private static IReadOnlyCollection<PersonalRecordDto> PersonalRecords(IEnumerable<WorkoutSession> sessions)
    {
        return sessions.SelectMany(session => session.ExerciseLogs.SelectMany(log => log.Sets
                .Where(set => set.WeightKg > 0)
                .Select(set => new { log.ExerciseId, log.Exercise.Name, Weight = set.WeightKg!.Value, AchievedAt = session.EndedAt ?? session.StartedAt })))
            .GroupBy(item => new { item.ExerciseId, item.Name })
            .Select(group => group.OrderByDescending(item => item.Weight).ThenBy(item => item.AchievedAt).First())
            .OrderByDescending(item => item.Weight).Take(10)
            .Select(item => new PersonalRecordDto(item.ExerciseId, item.Name, item.Weight, item.AchievedAt)).ToList();
    }
}
