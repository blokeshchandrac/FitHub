using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Workouts;
using FitHub.Application.Authentication;
using FitHub.Application.Workouts;
using FitHub.Application.Trainers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Trainer)]
[Route("api/trainer/workout-assignments")]
public sealed class TrainerWorkoutAssignmentsController(IWorkoutService workoutService, ITrainerMemberAccessService memberAccess) : ControllerBase
{
    [HttpGet("members")]
    public Task<IReadOnlyCollection<AssignedMemberDto>> Members(CancellationToken cancellationToken) =>
        memberAccess.GetAssignedMembersAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpGet]
    public Task<IReadOnlyCollection<WorkoutAssignmentDto>> Assignments(CancellationToken cancellationToken) =>
        workoutService.GetTrainerAssignmentsAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpPost]
    public Task<WorkoutAssignmentDto> Assign(AssignWorkoutTemplateRequest request, CancellationToken cancellationToken) =>
        workoutService.AssignTemplateAsync(User.GetRequiredUserId(), request.TemplateId, request.MemberUserId, cancellationToken);

    [HttpDelete("{assignmentId:guid}")]
    public async Task<IActionResult> Remove(Guid assignmentId, CancellationToken cancellationToken) =>
        await workoutService.RemoveAssignmentAsync(User.GetRequiredUserId(), assignmentId, cancellationToken)
            ? NoContent()
            : NotFound();
}
