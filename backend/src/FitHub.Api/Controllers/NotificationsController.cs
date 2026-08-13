using FitHub.Api.Infrastructure;using FitHub.Application.Authentication;using FitHub.Application.Notifications;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][Authorize(Policy=AuthorizationPolicies.ActiveMembership)][Route("api/notifications")]
public sealed class NotificationsController(INotificationService service):ControllerBase
{
    [HttpGet]public Task<IReadOnlyCollection<NotificationDto>> Get(CancellationToken ct)=>service.GetAsync(User.GetRequiredUserId(),ct);
    [HttpPost("{notificationId:guid}/read")]public async Task<IActionResult> Read(Guid notificationId,CancellationToken ct)=>await service.MarkReadAsync(User.GetRequiredUserId(),notificationId,ct)?NoContent():NotFound();
}
