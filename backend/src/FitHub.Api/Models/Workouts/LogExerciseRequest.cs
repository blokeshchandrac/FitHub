namespace FitHub.Api.Models.Workouts;

public sealed record WorkoutSetRequest(int Repetitions, decimal WeightKg, int? DurationSeconds);
public sealed record LogExerciseRequest(Guid ExerciseId, string? Notes, IReadOnlyCollection<WorkoutSetRequest> Sets);
