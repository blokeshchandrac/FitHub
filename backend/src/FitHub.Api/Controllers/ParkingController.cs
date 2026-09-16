using FitHub.Api.Infrastructure;using FitHub.Api.Models.Parking;using FitHub.Application.Authentication;using FitHub.Application.Parking;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][Route("api/parking")]
[Authorize(Roles=AuthorizationPolicies.Member+","+AuthorizationPolicies.Administrator)]
[Authorize(Policy=AuthorizationPolicies.ActiveMembership)]
public sealed class ParkingController(IParkingService service,IParkingSimulationService simulation):ControllerBase
{
    [HttpGet]public Task<ParkingOverviewDto> Overview(CancellationToken ct)=>service.GetOverviewAsync(ct);
    [HttpPost][Authorize(Policy=AuthorizationPolicies.Administrator)]public async Task<ActionResult<ParkingSpaceDto>> Create(CreateParkingSpaceRequest request,CancellationToken ct){var result=await service.CreateAsync(new(request.Code,request.Zone,request.IsAccessible),ct);return CreatedAtAction(nameof(Overview),result);}
    [HttpPost("{spaceId:guid}/correction")][Authorize(Policy=AuthorizationPolicies.Administrator)]public async Task<ActionResult<ParkingSpaceDto>> Correct(Guid spaceId,ParkingCorrectionRequest request,CancellationToken ct){var result=await service.CorrectAsync(spaceId,User.GetRequiredUserId(),new(request.State,request.Reason),ct);return result is null?NotFound():Ok(result);}
    [HttpGet("alerts")][Authorize(Policy=AuthorizationPolicies.Administrator)]public Task<IReadOnlyCollection<ParkingAlertDto>> Alerts([FromQuery]bool includeResolved=false,CancellationToken ct=default)=>simulation.GetAlertsAsync(includeResolved,ct);
}
