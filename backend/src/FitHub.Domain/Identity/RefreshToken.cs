using FitHub.Domain.Common;

namespace FitHub.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? createdByIp)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke(DateTimeOffset revokedAt, string? revokedByIp, string? replacedByTokenHash = null)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = revokedAt;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
        MarkUpdated(revokedAt);
    }
}
