using FitHub.Domain.Memberships;

namespace FitHub.UnitTests;

public sealed class MembershipTests
{
    [Fact]
    public void ConstructorRejectsEndBeforeStart()
    {
        var startsAt = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            new Membership(Guid.NewGuid(), Guid.NewGuid(), startsAt, startsAt.AddDays(-1)));
    }

    [Fact]
    public void RenewalExtendsAnActiveMembership()
    {
        var startsAt = DateTimeOffset.UtcNow;
        var membership = new Membership(Guid.NewGuid(), Guid.NewGuid(), startsAt, startsAt.AddMonths(1));
        membership.Activate(startsAt, startsAt.AddMonths(1));

        membership.Renew(startsAt.AddMonths(2));

        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(startsAt.AddMonths(2), membership.EndsAt);
    }
}
