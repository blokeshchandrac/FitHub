using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Workouts;
using FitHub.Application.Authentication;
using FitHub.Application.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Trainer)]
[Route("api/trainer/workout-plans")]
public sealed class TrainerWorkoutPlansController(IWorkoutService workoutService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<WorkoutTemplateDto>> Get(CancellationToken cancellationToken) =>
        workoutService.GetTrainerPlansAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpGet("exercises")]
    public Task<IReadOnlyCollection<ExerciseDto>> Exercises(
        [FromQuery] string? search, [FromQuery] string? muscleGroup, [FromQuery] string? equipment,
        CancellationToken cancellationToken) =>
        workoutService.SearchExercisesAsync(search, muscleGroup, equipment, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<WorkoutTemplateDto>> Create(
        UpsertWorkoutTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await workoutService.CreateTrainerPlanAsync(User.GetRequiredUserId(), ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<WorkoutTemplateDto>> Update(
        Guid templateId, UpsertWorkoutTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await workoutService.UpdateTrainerPlanAsync(User.GetRequiredUserId(), templateId, ToCommand(request), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private static UpsertWorkoutTemplateCommand ToCommand(UpsertWorkoutTemplateRequest request) =>
        new(request.Name, request.Description, false, request.ExerciseIds);
}
