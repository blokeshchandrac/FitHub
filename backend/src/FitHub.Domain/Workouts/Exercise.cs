using FitHub.Domain.Common;

namespace FitHub.Domain.Workouts;

public sealed class Exercise : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MuscleGroup { get; set; } = string.Empty;
    public string? Equipment { get; set; }
    public string? Instructions { get; set; }
    public ICollection<WorkoutTemplate> WorkoutTemplates { get; } = [];
}
