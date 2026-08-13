using FitHub.Api.Infrastructure;
using FitHub.Application.Authentication;
using FitHub.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Route("api/dashboard")]
public sealed class MemberDashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public Task<MemberDashboardDto> Get(CancellationToken cancellationToken) => dashboardService.GetDashboardAsync(User.GetRequiredUserId(), cancellationToken);
}
