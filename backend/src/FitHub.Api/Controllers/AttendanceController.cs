using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Attendance;
using FitHub.Application.Attendance;
using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/attendance")]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public Task<AttendanceSummaryDto> GetSummary(CancellationToken cancellationToken) =>
        attendanceService.GetSummaryAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpPost("check-in-token")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public Task<CheckInTokenDto> GenerateToken(CancellationToken cancellationToken) =>
        attendanceService.GenerateTokenAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpPost("check-in")]
    [Authorize(Policy = AuthorizationPolicies.Staff)]
    public async Task<ActionResult<CheckInResultDto>> CheckIn(CheckInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await attendanceService.CheckInAsync(request.Token, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
