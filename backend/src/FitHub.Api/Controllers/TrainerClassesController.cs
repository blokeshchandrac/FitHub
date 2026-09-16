using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Bookings;
using FitHub.Application.Authentication;
using FitHub.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Trainer)]
[Route("api/trainer/classes")]
public sealed class TrainerClassesController(IClassBookingService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ClassScheduleDto>> List(CancellationToken ct) =>
        service.ListForTrainerAsync(User.GetRequiredUserId(), ct);

    [HttpPost]
    public async Task<ActionResult<ClassScheduleDto>> Create(CreateClassScheduleRequest request, CancellationToken ct)
    {
        var trainerId = User.GetRequiredUserId();
        var result = await service.CreateAsync(ToCommand(request, trainerId), ct);
        return CreatedAtAction(nameof(List), result);
    }

    [HttpPut("{scheduleId:guid}")]
    public async Task<ActionResult<ClassScheduleDto>> Update(Guid scheduleId, CreateClassScheduleRequest request, CancellationToken ct)
    {
        var trainerId = User.GetRequiredUserId();
        var result = await service.UpdateAsync(scheduleId, trainerId, ToCommand(request, trainerId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{scheduleId:guid}/participants")]
    public async Task<ActionResult<IReadOnlyCollection<ClassParticipantDto>>> Participants(Guid scheduleId, CancellationToken ct)
    {
        try
        {
            var result = await service.ListParticipantsAsync(scheduleId, User.GetRequiredUserId(), false, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPut("{scheduleId:guid}/participants/{memberId:guid}/attendance")]
    public async Task<ActionResult<ClassScheduleDto>> Attendance(Guid scheduleId, Guid memberId, RecordClassAttendanceRequest request, CancellationToken ct)
    {
        try
        {
            var result = await service.RecordAttendanceAsync(scheduleId, memberId, User.GetRequiredUserId(), false, request.Attended, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private static CreateClassScheduleCommand ToCommand(CreateClassScheduleRequest request, Guid trainerId) =>
        new(request.Name, request.Description, request.StartsAt, request.EndsAt, request.Capacity, trainerId, request.Location);
}
