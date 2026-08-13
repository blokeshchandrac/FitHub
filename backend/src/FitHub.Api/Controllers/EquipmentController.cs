using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Equipment;
using FitHub.Application.Authentication;
using FitHub.Application.Equipment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/equipment")]
public sealed class EquipmentController(IEquipmentService equipmentService, IEquipmentAccessService accessService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<EquipmentSummaryDto>> List(CancellationToken cancellationToken)
    {
        var live = User.IsInRole(AuthorizationPolicies.Trainer) || User.IsInRole(AuthorizationPolicies.Administrator) || await accessService.IsCheckedInAsync(User.GetRequiredUserId(), cancellationToken);
        return await equipmentService.ListAsync(live, cancellationToken);
    }

    [HttpGet("{equipmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public async Task<ActionResult<EquipmentDetailDto>> Details(Guid equipmentId, CancellationToken cancellationToken)
    {
        if (!await CanUseLiveDetails(cancellationToken)) return Forbid();
        var result = await equipmentService.GetAsync(equipmentId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{equipmentId:guid}/claim")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public async Task<ActionResult<EquipmentDetailDto>> Claim(Guid equipmentId, CancellationToken cancellationToken) => await CheckedInChange(() => equipmentService.ClaimAsync(equipmentId, User.GetRequiredUserId(), cancellationToken), cancellationToken);

    [HttpPost("{equipmentId:guid}/sharing-allowed")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public async Task<ActionResult<EquipmentDetailDto>> Sharing(Guid equipmentId, CancellationToken cancellationToken) => await CheckedInChange(() => equipmentService.AllowSharingAsync(equipmentId, User.GetRequiredUserId(), cancellationToken), cancellationToken);

    [HttpPost("{equipmentId:guid}/occupied")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public async Task<ActionResult<EquipmentDetailDto>> Occupied(Guid equipmentId, CancellationToken cancellationToken) => await CheckedInChange(() => equipmentService.ReturnOccupiedAsync(equipmentId, User.GetRequiredUserId(), cancellationToken), cancellationToken);

    [HttpPost("{equipmentId:guid}/release")]
    [Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
    public async Task<ActionResult<EquipmentDetailDto>> Release(Guid equipmentId, ReleaseEquipmentRequest request, CancellationToken cancellationToken) => await CheckedInChange(() => equipmentService.ReleaseAsync(equipmentId, User.GetRequiredUserId(), request.ClearanceConfirmed, cancellationToken), cancellationToken);

    [HttpPost("{equipmentId:guid}/override")]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public async Task<ActionResult<EquipmentDetailDto>> Override(Guid equipmentId, EquipmentOverrideRequest request, CancellationToken cancellationToken)
    {
        var result = await equipmentService.OverrideAsync(equipmentId, User.GetRequiredUserId(), new(request.State, request.Reason), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private async Task<ActionResult<EquipmentDetailDto>> CheckedInChange(Func<Task<EquipmentDetailDto?>> action, CancellationToken ct)
    {
        if (!await CanUseLiveDetails(ct)) return Forbid();
        var result = await action(); return result is null ? NotFound() : Ok(result);
    }
    private async Task<bool> CanUseLiveDetails(CancellationToken ct) => User.IsInRole(AuthorizationPolicies.Trainer) || User.IsInRole(AuthorizationPolicies.Administrator) || await accessService.IsCheckedInAsync(User.GetRequiredUserId(), ct);
}
