using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Workouts;

public sealed class WorkoutTemplate : Entity
{
    public Guid? OwnerUserId { get; set; }
    public ApplicationUser? OwnerUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public WorkoutTemplateKind Kind { get; set; } = WorkoutTemplateKind.Personal;
    public ICollection<Exercise> Exercises { get; } = [];
}

public enum WorkoutTemplateKind
{
    Personal,
    TrainerPlan
}
