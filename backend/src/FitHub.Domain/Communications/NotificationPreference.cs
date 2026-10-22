using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Communications;

public sealed class NotificationPreference : Entity
{
    private NotificationPreference() { }

    public NotificationPreference(Guid userId, NotificationType category, bool enabled = true)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        UserId = userId;
        Category = category;
        Enabled = enabled;
    }

    public Guid UserId { get; private set; }
    public ApplicationUser User { get; private set; } = null!;
    public NotificationType Category { get; private set; }
    public bool Enabled { get; private set; }

    public void SetEnabled(bool enabled, DateTimeOffset changedAtUtc)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        MarkUpdated(changedAtUtc);
    }

    public static IEnumerable<NotificationPreference> CreateDefaults(Guid userId) =>
        Enum.GetValues<NotificationType>().Select(category => new NotificationPreference(userId, category));
}
