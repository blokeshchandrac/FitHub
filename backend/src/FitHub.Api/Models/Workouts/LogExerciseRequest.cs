namespace FitHub.Api.Models.Workouts;

public sealed record WorkoutSetRequest(int Repetitions, decimal WeightKg, int? DurationSeconds, int? RestSeconds);
public sealed record LogExerciseRequest(Guid ExerciseId, string? Notes, IReadOnlyCollection<WorkoutSetRequest> Sets);
