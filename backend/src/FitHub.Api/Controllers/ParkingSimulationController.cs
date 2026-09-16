using FitHub.Api.Models.Parking;
using FitHub.Api.Simulation;
using FitHub.Application.Authentication;
using FitHub.Application.Parking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[EnableRateLimiting("simulation")]
[Route("api/simulation/parking")]
public sealed class ParkingSimulationController(
    IOptions<SimulationOptions> options,
    IParkingSimulationService service,
    IHostEnvironment hostEnvironment,
    ILogger<ParkingSimulationController> logger) : ControllerBase
{
    private readonly SimulationOptions settings = options.Value;
    private bool IsAvailable => settings.Enabled &&
        (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"));

    [HttpGet("status")]
    public IActionResult Status() => IsAvailable
        ? Ok(new
        {
            enabled = true,
            hubUrl = "/hubs/parking",
            cameraId = ParkingSimulationConstants.CameraId,
            normalScanSeconds = settings.ParkingScanSeconds,
            demoScanSeconds = settings.ParkingDemoScanSeconds
        })
        : NotFound();

    [HttpPost("scans")]
    public async Task<ActionResult<ParkingSimulationScanResultDto>> Scan(ParkingSimulationScanRequest request, CancellationToken ct)
    {
        if (!IsAvailable) return NotFound();
        var command = new ParkingSimulationScanCommand(
            request.SimulationSessionId,
            request.SourceDeviceId,
            request.ObservedAt,
            request.Observations.Select(x => new ParkingSimulationObservationCommand(x.SpaceId, x.Status, x.Confidence)).ToArray(),
            request.Violations.Select(x => new ParkingSimulationViolationCommand(
                x.VehicleSimulationId, x.AffectedSpaceIds, x.ViolationType, x.Reason)).ToArray());
        var result = await service.RecordScanAsync(command, ct);
        logger.LogInformation(
            "Accepted simulated parking scan {SimulationSessionId}: {UpdatedSpaces} spaces, {ActiveAlerts} active alerts",
            request.SimulationSessionId,
            result.UpdatedSpaces,
            result.ActiveAlerts.Count);
        return Ok(result);
    }
}
