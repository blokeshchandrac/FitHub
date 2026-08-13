using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Attendance;

public sealed class AttendanceCheckInToken : Entity
{
    private AttendanceCheckInToken() { }

    public AttendanceCheckInToken(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public uint Version { get; private set; }

    public void Use(DateTimeOffset timestamp)
    {
        if (UsedAt is not null) throw new InvalidOperationException("Check-in token has already been used.");
        if (ExpiresAt <= timestamp) throw new InvalidOperationException("Check-in token has expired.");
        UsedAt = timestamp;
    }
}
