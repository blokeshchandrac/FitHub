using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Workouts;

public sealed class WorkoutPlanAssignment : Entity
{
    private WorkoutPlanAssignment() { }

    public WorkoutPlanAssignment(Guid workoutTemplateId, Guid memberUserId, Guid trainerUserId, DateTimeOffset assignedAt)
    {
        WorkoutTemplateId = workoutTemplateId;
        MemberUserId = memberUserId;
        TrainerUserId = trainerUserId;
        AssignedAt = assignedAt;
    }

    public Guid WorkoutTemplateId { get; private set; }
    public WorkoutTemplate WorkoutTemplate { get; private set; } = null!;
    public Guid MemberUserId { get; private set; }
    public ApplicationUser MemberUser { get; private set; } = null!;
    public Guid TrainerUserId { get; private set; }
    public ApplicationUser TrainerUser { get; private set; } = null!;
    public DateTimeOffset AssignedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void Deactivate(DateTimeOffset endedAt)
    {
        IsActive = false;
        EndedAt = endedAt;
        MarkUpdated(endedAt);
    }
}
