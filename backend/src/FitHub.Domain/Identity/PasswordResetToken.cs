using FitHub.Domain.Common;

namespace FitHub.Domain.Identity;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken() { }

    public PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        string? createdByIp)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public DateTimeOffset? InvalidatedAtUtc { get; private set; }
    public string? InvalidationReason { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool CanBeUsedAt(DateTimeOffset now) =>
        UsedAtUtc is null && InvalidatedAtUtc is null && ExpiresAtUtc > now;

    public void MarkUsed(DateTimeOffset usedAtUtc)
    {
        if (!CanBeUsedAt(usedAtUtc)) throw new InvalidOperationException("Password-reset token is not usable.");
        UsedAtUtc = usedAtUtc;
        MarkUpdated(usedAtUtc);
    }

    public void Invalidate(DateTimeOffset invalidatedAtUtc, string reason)
    {
        if (UsedAtUtc is not null || InvalidatedAtUtc is not null) return;
        InvalidatedAtUtc = invalidatedAtUtc;
        InvalidationReason = string.IsNullOrWhiteSpace(reason) ? "Invalidated" : reason.Trim();
        MarkUpdated(invalidatedAtUtc);
    }
}
