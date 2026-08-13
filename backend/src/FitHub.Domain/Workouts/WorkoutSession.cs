using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Workouts;

public sealed class WorkoutSession : Entity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public WorkoutSessionStatus Status { get; set; } = WorkoutSessionStatus.Planned;
    public string? Notes { get; set; }
    public ICollection<ExerciseLog> ExerciseLogs { get; } = [];

    public void Start(DateTimeOffset startedAt)
    {
        if (Status is not WorkoutSessionStatus.Planned) throw new InvalidOperationException("Only a planned workout can be started.");
        StartedAt = startedAt;
        Status = WorkoutSessionStatus.InProgress;
    }

    public void Complete(DateTimeOffset endedAt, string? notes = null)
    {
        if (Status is not WorkoutSessionStatus.InProgress) throw new InvalidOperationException("Only an active workout can be completed.");
        if (endedAt < StartedAt) throw new ArgumentException("Workout end cannot precede its start.", nameof(endedAt));
        EndedAt = endedAt;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = WorkoutSessionStatus.Completed;
    }
}
