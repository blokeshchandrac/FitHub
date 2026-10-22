using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Notifications;

public sealed record UpdateNotificationPreferencesRequest(
    [Required, MinLength(1)] IReadOnlyCollection<NotificationPreferenceItemRequest> Preferences);

public sealed record NotificationPreferenceItemRequest(
    [Required, StringLength(80)] string Category,
    bool Enabled);
