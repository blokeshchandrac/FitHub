using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Workouts;
using FitHub.Application.Authentication;
using FitHub.Application.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Route("api/workout-sessions")]
public sealed class WorkoutSessionsController(IWorkoutService workoutService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<WorkoutHistoryDto>> History(CancellationToken cancellationToken) => workoutService.GetHistoryAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<WorkoutSessionDto>> Details(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await workoutService.GetSessionAsync(User.GetRequiredUserId(), sessionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public Task<WorkoutSessionDto> Start([FromQuery] Guid? templateId, CancellationToken cancellationToken) => workoutService.StartSessionAsync(User.GetRequiredUserId(), templateId, cancellationToken);

    [HttpPut("{sessionId:guid}/exercise")]
    public async Task<ActionResult<WorkoutSessionDto>> LogExercise(Guid sessionId, LogExerciseRequest request, CancellationToken cancellationToken)
    {
        var command = new LogExerciseCommand(request.ExerciseId, request.Notes, request.Sets.Select(set => new WorkoutSetCommand(set.Repetitions, set.WeightKg, set.DurationSeconds)).ToList());
        var result = await workoutService.LogExerciseAsync(User.GetRequiredUserId(), sessionId, command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<WorkoutSessionDto>> Complete(Guid sessionId, CompleteWorkoutRequest request, CancellationToken cancellationToken)
    {
        var result = await workoutService.CompleteSessionAsync(User.GetRequiredUserId(), sessionId, request.Notes, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
