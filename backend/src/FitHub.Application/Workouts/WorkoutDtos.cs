using FitHub.Domain.Workouts;

namespace FitHub.Application.Workouts;

public sealed record ExerciseDto(Guid Id, string Name, string MuscleGroup, string? Equipment, string? Instructions);
public sealed record WorkoutTemplateDto(Guid Id, string Name, string? Description, bool IsPublic, Guid? OwnerUserId, IReadOnlyCollection<ExerciseDto> Exercises, bool IsAssigned);
public sealed record UpsertWorkoutTemplateCommand(string Name, string? Description, bool IsPublic, IReadOnlyCollection<Guid> ExerciseIds);
public sealed record WorkoutSetCommand(int Repetitions, decimal WeightKg, int? DurationSeconds);
public sealed record LogExerciseCommand(Guid ExerciseId, string? Notes, IReadOnlyCollection<WorkoutSetCommand> Sets);
public sealed record WorkoutSetDto(Guid Id, int SetNumber, int? Repetitions, decimal? WeightKg, int? DurationSeconds, decimal Volume, bool IsPersonalRecord);
public sealed record PreviousPerformanceDto(DateTimeOffset CompletedAt, decimal BestWeightKg, decimal Volume);
public sealed record WorkoutExerciseDto(Guid ExerciseLogId, ExerciseDto Exercise, string? Notes, PreviousPerformanceDto? PreviousPerformance, IReadOnlyCollection<WorkoutSetDto> Sets);
public sealed record WorkoutSessionDto(Guid Id, Guid? WorkoutTemplateId, string? TemplateName, WorkoutSessionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes, decimal Volume, IReadOnlyCollection<WorkoutExerciseDto> Exercises);
public sealed record WorkoutHistoryDto(Guid Id, string Name, WorkoutSessionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, decimal Volume);
public sealed record MemberSummaryDto(Guid Id, string Email, string FirstName, string LastName);
public sealed record WorkoutAssignmentDto(Guid Id, Guid TemplateId, string TemplateName, Guid MemberId, string MemberName, DateTimeOffset AssignedAt, bool IsActive);
