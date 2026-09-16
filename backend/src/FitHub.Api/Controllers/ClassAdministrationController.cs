using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Bookings;
using FitHub.Application.Authentication;
using FitHub.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/administration/classes")]
public sealed class ClassAdministrationController(IClassBookingService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ClassScheduleDto>> List(CancellationToken ct) => service.ListForAdministrationAsync(ct);

    [HttpPut("{scheduleId:guid}/review")]
    public async Task<ActionResult<ClassScheduleDto>> Review(Guid scheduleId, ReviewClassScheduleRequest request, CancellationToken ct)
    {
        var result = await service.ReviewAsync(scheduleId, User.GetRequiredUserId(), new(request.Approve, request.Reason), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{scheduleId:guid}/participants")]
    public async Task<ActionResult<IReadOnlyCollection<ClassParticipantDto>>> Participants(Guid scheduleId, CancellationToken ct)
    {
        var result = await service.ListParticipantsAsync(scheduleId, User.GetRequiredUserId(), true, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{scheduleId:guid}/participants/{memberId:guid}/attendance")]
    public async Task<ActionResult<ClassScheduleDto>> Attendance(Guid scheduleId, Guid memberId, RecordClassAttendanceRequest request, CancellationToken ct)
    {
        var result = await service.RecordAttendanceAsync(scheduleId, memberId, User.GetRequiredUserId(), true, request.Attended, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
