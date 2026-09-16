using FitHub.Api.Infrastructure;
using FitHub.Application.Authentication;
using FitHub.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Member)]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Route("api/classes")]
public sealed class ClassBookingsController(IClassBookingService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ClassScheduleDto>> List(CancellationToken ct) =>
        service.ListForMemberAsync(User.GetRequiredUserId(), ct);

    [HttpPost("{scheduleId:guid}/book")]
    public async Task<ActionResult<ClassScheduleDto>> Book(Guid scheduleId, CancellationToken ct)
    {
        var result = await service.BookAsync(scheduleId, User.GetRequiredUserId(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{scheduleId:guid}/cancel")]
    public async Task<ActionResult<ClassScheduleDto>> Cancel(Guid scheduleId, CancellationToken ct)
    {
        var result = await service.CancelAsync(scheduleId, User.GetRequiredUserId(), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
