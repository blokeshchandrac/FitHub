using FitHub.Domain.Attendance;

namespace FitHub.UnitTests;

public sealed class AttendanceCheckInTokenTests
{
    [Fact]
    public void ExpiredTokenCannotBeUsed()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new AttendanceCheckInToken(Guid.NewGuid(), new string('A', 64), now.AddSeconds(-1));

        var exception = Assert.Throws<InvalidOperationException>(() => token.Use(now));

        Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TokenCannotBeReplayed()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new AttendanceCheckInToken(Guid.NewGuid(), new string('A', 64), now.AddMinutes(1));
        token.Use(now);

        var exception = Assert.Throws<InvalidOperationException>(() => token.Use(now.AddSeconds(1)));

        Assert.Contains("already", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
