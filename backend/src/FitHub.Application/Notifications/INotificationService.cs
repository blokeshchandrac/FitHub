namespace FitHub.Application.Notifications;
public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationDto>> GetAsync(Guid userId, bool generateMemberNotifications = true, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task CreateFaultStatusAsync(Guid userId, Guid reportId, string equipmentName, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationPreferenceDto>> UpdatePreferencesAsync(Guid userId, IReadOnlyCollection<NotificationPreferenceUpdate> updates, CancellationToken cancellationToken = default);
}
