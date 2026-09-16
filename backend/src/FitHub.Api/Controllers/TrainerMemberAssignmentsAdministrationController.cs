using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Administration;
using FitHub.Application.Authentication;
using FitHub.Application.Trainers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/administration/trainers/{trainerId:guid}/assignments")]
public sealed class TrainerMemberAssignmentsAdministrationController(ITrainerMemberAssignmentService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<TrainerMemberAssignmentDto>> Get(
        Guid trainerId, [FromQuery] bool includeEnded = true, CancellationToken cancellationToken = default) =>
        service.GetAssignmentsAsync(trainerId, includeEnded, cancellationToken);

    [HttpGet("eligible-members")]
    public Task<IReadOnlyCollection<EligibleMemberDto>> EligibleMembers(
        Guid trainerId, [FromQuery] string? search, CancellationToken cancellationToken = default) =>
        service.SearchEligibleMembersAsync(trainerId, search, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<TrainerMemberAssignmentDto>> Assign(
        Guid trainerId, AssignTrainerMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await service.AssignAsync(User.GetRequiredUserId(), trainerId, request.MemberId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { trainerId }, result);
    }

    [HttpDelete("{assignmentId:guid}")]
    public async Task<IActionResult> Unassign(Guid trainerId, Guid assignmentId, CancellationToken cancellationToken) =>
        await service.UnassignAsync(User.GetRequiredUserId(), trainerId, assignmentId, cancellationToken)
            ? NoContent()
            : NotFound();
}
