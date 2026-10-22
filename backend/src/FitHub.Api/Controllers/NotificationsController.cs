using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Notifications;
using FitHub.Application.Authentication;
using FitHub.Application.Common;
using FitHub.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<NotificationDto>> Get(CancellationToken ct) =>
        service.GetAsync(User.GetRequiredUserId(), User.IsInRole(AuthorizationPolicies.Member), ct);

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> Read(Guid notificationId, CancellationToken ct) =>
        await service.MarkReadAsync(User.GetRequiredUserId(), notificationId, ct) ? NoContent() : NotFound();

    [HttpGet("preferences")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<IReadOnlyCollection<NotificationPreferenceDto>> GetPreferences(CancellationToken ct) =>
        service.GetPreferencesAsync(User.GetRequiredUserId(), ct);

    [HttpPut("preferences")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<IReadOnlyCollection<NotificationPreferenceDto>> UpdatePreferences(
        UpdateNotificationPreferencesRequest request,
        CancellationToken ct)
    {
        var duplicates = request.Preferences.GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
            throw new CommandValidationException(new Dictionary<string, string[]>
            {
                ["preferences"] = ["Each notification category may be supplied only once."]
            });

        return service.UpdatePreferencesAsync(
            User.GetRequiredUserId(),
            request.Preferences.Select(item => new NotificationPreferenceUpdate(item.Category, item.Enabled)).ToArray(),
            ct);
    }
}
