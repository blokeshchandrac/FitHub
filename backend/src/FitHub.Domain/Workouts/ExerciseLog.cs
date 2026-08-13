using FitHub.Domain.Common;

namespace FitHub.Domain.Workouts;

public sealed class ExerciseLog : Entity
{
    public Guid WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int OrderIndex { get; set; }
    public string? Notes { get; set; }
    public ICollection<SetLog> Sets { get; } = [];
}
