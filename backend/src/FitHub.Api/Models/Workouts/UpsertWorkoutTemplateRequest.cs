namespace FitHub.Api.Models.Workouts;

public sealed record UpsertWorkoutTemplateRequest(string Name, string? Description, bool IsPublic, IReadOnlyCollection<Guid> ExerciseIds);
