using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Memberships;

public sealed class Membership : Entity
{
    private Membership() { }

    public Membership(Guid userId, Guid membershipPlanId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Membership end must be after its start.", nameof(endsAt));
        }

        UserId = userId;
        MembershipPlanId = membershipPlanId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Status = MembershipStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public Guid MembershipPlanId { get; private set; }
    public MembershipPlan MembershipPlan { get; private set; } = null!;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public MembershipStatus Status { get; private set; }

    public bool IsActiveAt(DateTimeOffset timestamp) =>
        Status == MembershipStatus.Active && StartsAt <= timestamp && EndsAt > timestamp;

    public void Activate(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        EnsurePeriod(startsAt, endsAt);
        StartsAt = startsAt;
        EndsAt = endsAt;
        Status = MembershipStatus.Active;
    }

    public void Renew(DateTimeOffset endsAt)
    {
        if (endsAt <= EndsAt) throw new ArgumentException("Renewal must extend the membership.", nameof(endsAt));
        EndsAt = endsAt;
        Status = MembershipStatus.Active;
    }

    public void Upgrade(Guid membershipPlanId, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        EnsurePeriod(startsAt, endsAt);
        MembershipPlanId = membershipPlanId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Status = MembershipStatus.Active;
    }

    public void MarkExpired() => Status = MembershipStatus.Expired;

    private static void EnsurePeriod(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (endsAt <= startsAt) throw new ArgumentException("Membership end must be after its start.", nameof(endsAt));
    }
}
