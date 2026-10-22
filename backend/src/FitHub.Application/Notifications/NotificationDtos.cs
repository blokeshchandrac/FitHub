namespace FitHub.Application.Notifications;
public sealed record NotificationDto(Guid Id,string Type,string Title,string Message,bool IsRead,DateTimeOffset CreatedAt,DateTimeOffset? ReadAt);

public sealed record NotificationPreferenceDto(
    string Category,
    string DisplayName,
    string Description,
    bool Enabled,
    bool CanDisable);

public sealed record NotificationPreferenceUpdate(string Category, bool Enabled);
