using FitHub.Domain.Common;

namespace FitHub.Domain.Identity;

public enum TrainerMemberAssignmentStatus
{
    Active,
    Ended
}

public sealed class TrainerMemberAssignment : Entity
{
    private TrainerMemberAssignment() { }

    public TrainerMemberAssignment(
        Guid trainerId,
        Guid memberId,
        Guid assignedByAdministratorId,
        DateTimeOffset assignedAt)
    {
        if (trainerId == Guid.Empty) throw new ArgumentException("Trainer is required.", nameof(trainerId));
        if (memberId == Guid.Empty) throw new ArgumentException("Member is required.", nameof(memberId));
        if (assignedByAdministratorId == Guid.Empty) throw new ArgumentException("Administrator is required.", nameof(assignedByAdministratorId));
        if (trainerId == memberId) throw new ArgumentException("A trainer cannot be assigned to themselves.");

        TrainerId = trainerId;
        MemberId = memberId;
        AssignedByAdministratorId = assignedByAdministratorId;
        AssignedAt = assignedAt;
    }

    public Guid TrainerId { get; private set; }
    public ApplicationUser Trainer { get; private set; } = null!;
    public Guid MemberId { get; private set; }
    public ApplicationUser Member { get; private set; } = null!;
    public TrainerMemberAssignmentStatus Status { get; private set; } = TrainerMemberAssignmentStatus.Active;
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid AssignedByAdministratorId { get; private set; }
    public ApplicationUser AssignedByAdministrator { get; private set; } = null!;
    public DateTimeOffset? UnassignedAt { get; private set; }
    public Guid? UnassignedByAdministratorId { get; private set; }
    public ApplicationUser? UnassignedByAdministrator { get; private set; }

    public void End(Guid administratorId, DateTimeOffset endedAt)
    {
        if (Status != TrainerMemberAssignmentStatus.Active)
            throw new InvalidOperationException("The trainer assignment has already ended.");
        if (administratorId == Guid.Empty) throw new ArgumentException("Administrator is required.", nameof(administratorId));
        Status = TrainerMemberAssignmentStatus.Ended;
        UnassignedAt = endedAt;
        UnassignedByAdministratorId = administratorId;
        MarkUpdated(endedAt);
    }
}
