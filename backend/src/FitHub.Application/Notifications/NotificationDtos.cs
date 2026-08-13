namespace FitHub.Application.Notifications;
public sealed record NotificationDto(Guid Id,string Type,string Title,string Message,bool IsRead,DateTimeOffset CreatedAt,DateTimeOffset? ReadAt);
