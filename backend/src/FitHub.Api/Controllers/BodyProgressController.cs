using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Progress;
using FitHub.Application.Authentication;
using FitHub.Application.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Authorize(Policy = AuthorizationPolicies.Member)]
[Route("api/progress")]
public sealed class BodyProgressController(IProgressService progressService) : ControllerBase
{
    [HttpGet]
    public Task<BodyProgressDto> Get(CancellationToken cancellationToken) => progressService.GetProgressAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpPost]
    public async Task<ActionResult<BodyMeasurementDto>> Log(LogBodyMeasurementRequest request, CancellationToken cancellationToken)
    {
        var command = new BodyMeasurementCommand(request.WeightKg, request.HeightCm, request.BodyFatPercentage, request.ChestCm, request.WaistCm, request.HipCm, request.Notes);
        var result = await progressService.LogMeasurementAsync(User.GetRequiredUserId(), command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
}
