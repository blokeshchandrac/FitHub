namespace FitHub.Application.Analytics;

public static class AnalyticsRules
{
    public static decimal WorkoutVolume(IEnumerable<(int? Repetitions, decimal? WeightKg)> sets) =>
        sets.Sum(set => Math.Max(0, set.Repetitions ?? 0) * Math.Max(0m, set.WeightKg ?? 0m));

    public static bool IsPersonalRecord(decimal currentWeightKg, decimal? previousBestWeightKg) =>
        currentWeightKg > 0 && (previousBestWeightKg is null || currentWeightKg > previousBestWeightKg.Value);

    public static decimal? Bmi(decimal? heightCm, decimal? weightKg)
    {
        if (heightCm is null or <= 0 || weightKg is null or <= 0) return null;
        var heightMetres = heightCm.Value / 100m;
        return decimal.Round(weightKg.Value / (heightMetres * heightMetres), 1, MidpointRounding.AwayFromZero);
    }

    public static int CurrentWeeklyStreak(IEnumerable<DateTimeOffset> completedAt, DateTimeOffset now)
    {
        var activeWeeks = completedAt.Select(StartOfUtcWeek).ToHashSet();
        var week = StartOfUtcWeek(now);
        var streak = 0;
        while (activeWeeks.Contains(week))
        {
            streak++;
            week = week.AddDays(-7);
        }
        return streak;
    }

    public static decimal ConsistencyScore(IEnumerable<DateTimeOffset> completedAt, DateTimeOffset now, int windowWeeks = 8)
    {
        if (windowWeeks <= 0) throw new ArgumentOutOfRangeException(nameof(windowWeeks));
        var currentWeek = StartOfUtcWeek(now);
        var firstWeek = currentWeek.AddDays(-7 * (windowWeeks - 1));
        var activeWeeks = completedAt.Select(StartOfUtcWeek)
            .Where(week => week >= firstWeek && week <= currentWeek)
            .Distinct()
            .Count();
        return decimal.Round(activeWeeks * 100m / windowWeeks, 1, MidpointRounding.AwayFromZero);
    }

    public static DateTimeOffset StartOfUtcWeek(DateTimeOffset value)
    {
        var date = value.UtcDateTime.Date;
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return new DateTimeOffset(date.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }
}
