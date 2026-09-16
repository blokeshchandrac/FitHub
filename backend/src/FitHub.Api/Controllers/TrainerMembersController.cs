using FitHub.Api.Infrastructure;
using FitHub.Application.Authentication;
using FitHub.Application.Profiles;
using FitHub.Application.Trainers;
using FitHub.Application.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Trainer)]
[Route("api/trainer/members")]
public sealed class TrainerMembersController(
    ITrainerMemberAccessService memberAccess,
    IProfileService profileService,
    IWorkoutService workoutService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<AssignedMemberDto>> Get(CancellationToken cancellationToken) =>
        memberAccess.GetAssignedMembersAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpGet("{memberId:guid}/profile")]
    public async Task<ActionResult<MemberProfileDto>> Profile(Guid memberId, CancellationToken cancellationToken)
    {
        await memberAccess.EnsureAssignedAsync(User.GetRequiredUserId(), memberId, cancellationToken);
        var result = await profileService.GetAsync(memberId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{memberId:guid}/workouts")]
    public async Task<ActionResult<IReadOnlyCollection<WorkoutHistoryDto>>> Workouts(Guid memberId, CancellationToken cancellationToken)
    {
        await memberAccess.EnsureAssignedAsync(User.GetRequiredUserId(), memberId, cancellationToken);
        return Ok(await workoutService.GetHistoryAsync(memberId, cancellationToken));
    }
}
