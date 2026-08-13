namespace FitHub.Application.Dashboard;

public sealed record PersonalRecordDto(Guid ExerciseId, string ExerciseName, decimal WeightKg, DateTimeOffset AchievedAt);
public sealed record WeightTrendDto(decimal? LatestWeightKg, decimal? ChangeKg, IReadOnlyCollection<WeightTrendPointDto> Points);
public sealed record WeightTrendPointDto(DateTimeOffset RecordedAt, decimal WeightKg);
public sealed record MemberDashboardDto(decimal AttendanceFrequencyPerWeek, decimal WorkoutFrequencyPerWeek, decimal WeeklyVolume, IReadOnlyCollection<PersonalRecordDto> PersonalRecords, int CurrentWorkoutStreakWeeks, decimal ConsistencyScore, WeightTrendDto WeightTrend, int MembershipVisits, decimal? MembershipCostPerVisit);
