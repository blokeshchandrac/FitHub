using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Administration;
using FitHub.Application.Administration;
using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/administration")]
public sealed class AdministrationManagementController(IAdministrationManagementService service) : ControllerBase
{
    [HttpGet("users")]
    public Task<IReadOnlyCollection<AdminUserSummaryDto>> Users(
        [FromQuery] string? search, [FromQuery] string? role, [FromQuery] bool? active, CancellationToken ct) =>
        service.ListUsersAsync(search, role, active, ct);

    [HttpGet("users/{userId:guid}")]
    public async Task<ActionResult<AdminUserDetailsDto>> UserDetails(Guid userId, CancellationToken ct)
    {
        var result = await service.GetUserAsync(userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("users/{userId:guid}/active")]
    public async Task<IActionResult> SetUserActive(Guid userId, SetActiveRequest request, CancellationToken ct) =>
        await service.SetUserActiveAsync(User.GetRequiredUserId(), userId, request.Active, ct) ? NoContent() : NotFound();

    [HttpPut("memberships/{membershipId:guid}/enabled")]
    public async Task<IActionResult> SetMembershipEnabled(Guid membershipId, SetEnabledRequest request, CancellationToken ct) =>
        await service.SetMembershipEnabledAsync(User.GetRequiredUserId(), membershipId, request.Enabled, ct) ? NoContent() : NotFound();

    [HttpPut("cards/{cardId:guid}/active")]
    public async Task<IActionResult> SetCardActive(Guid cardId, SetActiveRequest request, CancellationToken ct) =>
        await service.SetCardActiveAsync(User.GetRequiredUserId(), cardId, request.Active, ct) ? NoContent() : NotFound();

    [HttpGet("trainers")]
    public Task<IReadOnlyCollection<AdminUserSummaryDto>> Trainers(CancellationToken ct) => service.ListTrainersAsync(ct);

    [HttpPost("trainers")]
    public async Task<ActionResult<AdminUserSummaryDto>> CreateTrainer(CreateTrainerRequest request, CancellationToken ct)
    {
        var trainer = await service.CreateTrainerAsync(User.GetRequiredUserId(), new(
            request.Email, request.FirstName, request.LastName, request.TemporaryPassword), ct);
        return CreatedAtAction(nameof(UserDetails), new { userId = trainer.Id }, trainer);
    }

    [HttpGet("activity")]
    public Task<IReadOnlyCollection<SystemActivityDto>> Activity(CancellationToken ct) => service.ListActivityAsync(ct);

    [HttpGet("payments")]
    public Task<IReadOnlyCollection<AdminPaymentDto>> Payments(CancellationToken ct) => service.ListPaymentsAsync(ct);
}
