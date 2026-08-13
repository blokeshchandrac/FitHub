using FitHub.Application.Authentication;
using FitHub.Application.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Route("api/exercises")]
public sealed class ExercisesController(IWorkoutService workoutService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ExerciseDto>> Search([FromQuery] string? search, [FromQuery] string? muscleGroup, [FromQuery] string? equipment, CancellationToken cancellationToken) =>
        workoutService.SearchExercisesAsync(search, muscleGroup, equipment, cancellationToken);
}
