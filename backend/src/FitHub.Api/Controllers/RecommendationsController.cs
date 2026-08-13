using FitHub.Api.Infrastructure;using FitHub.Api.Models.Recommendations;using FitHub.Application.Authentication;using FitHub.Application.Recommendations;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][Authorize(Policy=AuthorizationPolicies.ActiveMembership)][Route("api/recommendations")]
public sealed class RecommendationsController(IRecommendationService service):ControllerBase
{
    [HttpPost("generate")]public Task<RecommendationOverviewDto> Generate(CancellationToken ct)=>service.GenerateAsync(User.GetRequiredUserId(),ct);
    [HttpPut("preferences")]public Task<RecommendationOverviewDto> Preferences(UpdateRecommendationPreferencesRequest request,CancellationToken ct)=>service.UpdatePreferencesAsync(User.GetRequiredUserId(),new(request.FitnessGoal,request.ActivityLevel),ct);
}
