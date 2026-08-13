using FitHub.Api.Models.Equipment;
using FitHub.Application.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/device/equipment/{equipmentId:guid}")]
public sealed class EquipmentDeviceController(IEquipmentService equipmentService, IDeviceAuthenticator authenticator) : ControllerBase
{
    [HttpPost("claim")]
    public async Task<IActionResult> Claim(Guid equipmentId, DeviceCardRequest request, CancellationToken ct) { if (!await Authorized(equipmentId, ct)) return Unauthorized(); await equipmentService.DeviceClaimAsync(equipmentId, request.CardUid, ct); return NoContent(); }
    [HttpPost("action")]
    public async Task<IActionResult> Action(Guid equipmentId, DeviceActionRequest request, CancellationToken ct) { if (!await Authorized(equipmentId, ct)) return Unauthorized(); await equipmentService.DeviceActionAsync(equipmentId, request.Action, request.CardUid, ct); return NoContent(); }
    [HttpPost("sensor")]
    public async Task<IActionResult> Sensor(Guid equipmentId, SensorEventRequest request, CancellationToken ct) { if (!await Authorized(equipmentId, ct)) return Unauthorized(); await equipmentService.SensorAsync(equipmentId, request.Detected, ct); return NoContent(); }
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid equipmentId, CancellationToken ct) { if (!await Authorized(equipmentId, ct)) return Unauthorized(); await equipmentService.HeartbeatAsync(equipmentId, ct); return NoContent(); }
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(Guid equipmentId, CancellationToken ct) { if (!await Authorized(equipmentId, ct)) return Unauthorized(); await equipmentService.DisconnectAsync(equipmentId, ct); return NoContent(); }
    private Task<bool> Authorized(Guid equipmentId, CancellationToken ct) => authenticator.AuthenticateAsync(equipmentId, Request.Headers["X-FitHub-Device-Id"].ToString(), Request.Headers["X-FitHub-Device-Key"].ToString(), ct);
}
