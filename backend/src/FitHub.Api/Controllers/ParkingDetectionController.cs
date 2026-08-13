using FitHub.Api.Models.Parking;using FitHub.Application.Parking;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][AllowAnonymous][Route("api/parking/detection")]
public sealed class ParkingDetectionController(IParkingService service,IParkingCameraAuthenticator authenticator):ControllerBase
{
    [HttpGet("spaces")]public async Task<ActionResult<ParkingOverviewDto>> Spaces(CancellationToken ct){if(!await Authorized(ct))return Unauthorized();return Ok(await service.GetCameraOverviewAsync(ct));}
    [HttpPost("events")]public async Task<ActionResult<ParkingSpaceDto>> Event(ParkingDetectionRequest request,CancellationToken ct){var camera=CameraCode;if(!await authenticator.AuthenticateAsync(camera,Request.Headers["X-FitHub-Camera-Key"].ToString(),ct))return Unauthorized();return Ok(await service.RecordDetectionAsync(camera,new(request.SpaceCode,request.State,request.Confidence,request.Source,request.EventTime),ct));}
    private string CameraCode=>Request.Headers["X-FitHub-Camera-Id"].ToString();
    private Task<bool> Authorized(CancellationToken ct)=>authenticator.AuthenticateAsync(CameraCode,Request.Headers["X-FitHub-Camera-Key"].ToString(),ct);
}
