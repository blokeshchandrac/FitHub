using FitHub.Api.Models.Memberships;
using FitHub.Application.Authentication;
using FitHub.Application.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/administration/membership-plans")]
public sealed class MembershipPlansAdministrationController(IMembershipService membershipService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<MembershipPlanDto>> GetPlans(CancellationToken cancellationToken) =>
        membershipService.GetPlansAsync(true, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<MembershipPlanDto>> Create(UpsertMembershipPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await membershipService.CreatePlanAsync(ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetPlans), new { id = plan.Id }, plan);
    }

    [HttpPut("{planId:guid}")]
    public async Task<ActionResult<MembershipPlanDto>> Update(Guid planId, UpsertMembershipPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await membershipService.UpdatePlanAsync(planId, ToCommand(request), cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPut("{planId:guid}/active")]
    public async Task<IActionResult> SetActive(Guid planId, [FromQuery] bool active, CancellationToken cancellationToken) =>
        await membershipService.SetPlanActiveAsync(planId, active, cancellationToken) ? NoContent() : NotFound();

    private static UpsertMembershipPlanCommand ToCommand(UpsertMembershipPlanRequest request) =>
        new(request.Name, request.Description, request.Price, request.BillingPeriodMonths);
}
