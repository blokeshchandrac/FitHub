using FitHub.Domain.Common;

namespace FitHub.Domain.Workouts;

public sealed class SetLog : Entity
{
    public Guid ExerciseLogId { get; set; }
    public ExerciseLog ExerciseLog { get; set; } = null!;
    public int SetNumber { get; set; }
    public int? Repetitions { get; set; }
    public decimal? WeightKg { get; set; }
    public int? DurationSeconds { get; set; }
    public decimal Volume => (Repetitions ?? 0) * (WeightKg ?? 0m);
}
