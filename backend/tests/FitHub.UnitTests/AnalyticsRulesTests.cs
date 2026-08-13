using FitHub.Application.Analytics;

namespace FitHub.UnitTests;

public sealed class AnalyticsRulesTests
{
    [Fact]
    public void WorkoutVolumeSumsRepetitionsTimesWeightForEverySet()
    {
        var sets = new[] { (Repetitions: (int?)10, WeightKg: (decimal?)50m), (8, 60m), (null, 100m) };
        Assert.Equal(980m, AnalyticsRules.WorkoutVolume(sets));
    }

    [Fact]
    public void PersonalRecordRequiresWeightAbovePreviousBest()
    {
        Assert.True(AnalyticsRules.IsPersonalRecord(101m, 100m));
        Assert.False(AnalyticsRules.IsPersonalRecord(100m, 100m));
        Assert.True(AnalyticsRules.IsPersonalRecord(20m, null));
    }

    [Fact]
    public void CurrentStreakCountsConsecutiveUtcWeeksEndingThisWeek()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        var workouts = new[] { now, now.AddDays(-7), now.AddDays(-14), now.AddDays(-28) };
        Assert.Equal(3, AnalyticsRules.CurrentWeeklyStreak(workouts, now));
    }

    [Fact]
    public void ConsistencyScoreUsesDistinctActiveWeeksInEightWeekWindow()
    {
        var now = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
        var workouts = new[] { now, now.AddDays(-1), now.AddDays(-7), now.AddDays(-21) };
        Assert.Equal(37.5m, AnalyticsRules.ConsistencyScore(workouts, now));
    }

    [Theory]
    [InlineData(180, 81, 25.0)]
    [InlineData(165, 60, 22.0)]
    public void BmiUsesLatestHeightAndWeight(decimal heightCm, decimal weightKg, decimal expected) =>
        Assert.Equal(expected, AnalyticsRules.Bmi(heightCm, weightKg));
}
