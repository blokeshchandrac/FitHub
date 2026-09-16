using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;

namespace FitHub.UnitTests;

public sealed class RoleWorkflowDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AccessCardCanBeBlockedAndUnblocked()
    {
        var card = new MemberAccessCard(Guid.NewGuid(), "hash");
        card.Block(Now);
        Assert.False(card.IsActive);
        card.Unblock(Now.AddMinutes(1));
        Assert.True(card.IsActive);
    }

    [Fact]
    public void MembershipCanBeAdministrativelyPausedAndReactivatedBeforeExpiry()
    {
        var membership = new Membership(Guid.NewGuid(), Guid.NewGuid(), Now.AddDays(-1), Now.AddDays(20));
        membership.Activate(Now.AddDays(-1), Now.AddDays(20));
        membership.Pause();
        Assert.Equal(MembershipStatus.Paused, membership.Status);
        membership.Reactivate(Now);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public void ProfileCompletionStoresValidatedMemberInputs()
    {
        var user = new ApplicationUser { FirstName = "Taylor", LastName = "Member" };
        var branchId = Guid.NewGuid();
        user.UpdateProfile("Tay", new DateOnly(1998, 5, 20), 172.4m, 70.3m,
            FitnessGoal.Strength, ActivityLevel.Moderate, ExperienceLevel.Intermediate,
            [TrainingDay.Monday, TrainingDay.Thursday], branchId, Now);
        Assert.True(user.OnboardingCompleted);
        Assert.Equal("Tay", user.PreferredName);
        Assert.Equal(172.4m, user.HeightCm);
        Assert.Equal(ExperienceLevel.Intermediate, user.ExperienceLevel);
        Assert.Equal([TrainingDay.Monday, TrainingDay.Thursday], user.GetPlannedTrainingDays());
        Assert.Equal(branchId, user.PreferredGymBranchId);
    }

    [Fact]
    public void ProfileRejectsEmptyOrDuplicateTrainingDays()
    {
        var user = new ApplicationUser { FirstName = "Taylor", LastName = "Member" };
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("Tay", new DateOnly(1998, 5, 20), 172m, 70m,
            FitnessGoal.Strength, ActivityLevel.Moderate, ExperienceLevel.Beginner, [], Guid.NewGuid(), Now));
        Assert.Throws<ArgumentException>(() => user.UpdateProfile("Tay", new DateOnly(1998, 5, 20), 172m, 70m,
            FitnessGoal.Strength, ActivityLevel.Moderate, ExperienceLevel.Beginner,
            [TrainingDay.Monday, TrainingDay.Monday], Guid.NewGuid(), Now));
    }

    [Fact]
    public void TrainerAssignmentPreservesUnassignmentHistory()
    {
        var administratorId = Guid.NewGuid();
        var assignment = new TrainerMemberAssignment(Guid.NewGuid(), Guid.NewGuid(), administratorId, Now);
        assignment.End(administratorId, Now.AddDays(10));
        Assert.Equal(TrainerMemberAssignmentStatus.Ended, assignment.Status);
        Assert.Equal(Now.AddDays(10), assignment.UnassignedAt);
        Assert.Equal(administratorId, assignment.UnassignedByAdministratorId);
    }

    [Fact]
    public void TrainerCannotBeAssignedToThemselves()
    {
        var sameUserId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new TrainerMemberAssignment(
            sameUserId, sameUserId, Guid.NewGuid(), Now));
    }

    [Fact]
    public void RefundAmountIsCumulativeAndCannotMoveBackwards()
    {
        var payment = new Payment { UserId = Guid.NewGuid(), MembershipPlanId = Guid.NewGuid(), Amount = 49.90m };
        payment.MarkSucceeded(Now, "pi_refund_unit");
        payment.RecordRefund(20m, Now.AddMinutes(1));
        payment.RecordRefund(10m, Now.AddMinutes(2));
        Assert.Equal(20m, payment.RefundedAmount);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        payment.RecordRefund(49.90m, Now.AddMinutes(3));
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }
}
