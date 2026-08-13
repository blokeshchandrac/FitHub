using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Route("api/authorization")]
public sealed class AuthorizationController : ControllerBase
{
    [HttpGet("member")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public IActionResult Member() => Ok(new { role = AuthorizationPolicies.Member });

    [HttpGet("trainer")]
    [Authorize(Policy = AuthorizationPolicies.Trainer)]
    public IActionResult Trainer() => Ok(new { role = AuthorizationPolicies.Trainer });

    [HttpGet("administrator")]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public IActionResult Administrator() => Ok(new { role = AuthorizationPolicies.Administrator });
}
