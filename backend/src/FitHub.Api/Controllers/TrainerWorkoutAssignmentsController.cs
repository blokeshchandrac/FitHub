using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Workouts;
using FitHub.Application.Authentication;
using FitHub.Application.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Route("api/trainer/workout-assignments")]
public sealed class TrainerWorkoutAssignmentsController(IWorkoutService workoutService) : ControllerBase
{
    [HttpGet("members")]
    public Task<IReadOnlyCollection<MemberSummaryDto>> Members(CancellationToken cancellationToken) => workoutService.GetMembersAsync(cancellationToken);

    [HttpPost]
    public Task<WorkoutAssignmentDto> Assign(AssignWorkoutTemplateRequest request, CancellationToken cancellationToken) =>
        workoutService.AssignTemplateAsync(User.GetRequiredUserId(), request.TemplateId, request.MemberUserId, cancellationToken);
}
