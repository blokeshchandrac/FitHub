using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Profiles;
using FitHub.Application.Authentication;
using FitHub.Application.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Member)]
[Route("api/profile")]
public sealed class ProfileController(IProfileService service) : ControllerBase
{
    [HttpGet("branches")]
    public Task<IReadOnlyCollection<GymBranchDto>> Branches(CancellationToken ct) => service.GetActiveBranchesAsync(ct);

    [HttpGet]
    public async Task<ActionResult<MemberProfileDto>> Get(CancellationToken ct)
    {
        var profile = await service.GetAsync(User.GetRequiredUserId(), ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<MemberProfileDto>> Update(UpdateMemberProfileRequest request, CancellationToken ct)
    {
        var profile = await service.UpdateAsync(User.GetRequiredUserId(), new(
            request.PreferredName, request.DateOfBirth, request.HeightCm, request.WeightKg,
            request.FitnessGoal, request.ActivityLevel, request.ExperienceLevel,
            request.PlannedTrainingDays, request.PreferredGymBranchId), ct);
        return profile is null ? NotFound() : Ok(profile);
    }
}
