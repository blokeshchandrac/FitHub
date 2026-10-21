using System.Security.Claims;
using FitHub.Api.Models.Authentication;
using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FitHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(
    IAuthenticationService authenticationService,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshTokenCookie = "fithub.refresh";
    private const string InvalidCredentialsMessage = "Email or password is incorrect.";
    private const string RegistrationFailedMessage = "Registration could not be completed with the supplied details.";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.ConsentAccepted)
        {
            ModelState.AddModelError(nameof(request.ConsentAccepted),
                "You must accept the FitHub Terms of Use and acknowledge the Privacy Notice.");
            return ValidationProblem(ModelState);
        }

        var result = await authenticationService.RegisterAsync(
            new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName, request.ConsentAccepted),
            GetIpAddress(),
            cancellationToken);
        if (!result.Succeeded || result.Tokens is null)
        {
            return BadRequest(new { message = RegistrationFailedMessage });
        }

        WriteRefreshTokenCookie(result.Tokens);
        return Ok(ToResponse(result.Tokens));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            new LoginCommand(request.Email, request.Password),
            GetIpAddress(),
            cancellationToken);
        if (!result.Succeeded || result.Tokens is null)
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        WriteRefreshTokenCookie(result.Tokens);
        return Ok(ToResponse(result.Tokens));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticationResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { message = "The session could not be refreshed." });
        }

        var result = await authenticationService.RefreshAsync(refreshToken, GetIpAddress(), cancellationToken);
        if (!result.Succeeded || result.Tokens is null)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { message = "The session could not be refreshed." });
        }

        WriteRefreshTokenCookie(result.Tokens);
        return Ok(ToResponse(result.Tokens));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshTokenCookie, out var refreshToken);
        await authenticationService.LogoutAsync(refreshToken, GetCurrentUserId(), GetIpAddress(), cancellationToken);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> CurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var currentUser = await authenticationService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return currentUser is null ? Unauthorized() : Ok(currentUser);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("password-recovery")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authenticationService.RequestPasswordResetAsync(request.Email, GetIpAddress(), cancellationToken);
        return Accepted(new
        {
            message = "If an account matches that email, password recovery instructions will be sent."
        });
    }

    private void WriteRefreshTokenCookie(AuthenticationTokens tokens)
    {
        Response.Cookies.Append(RefreshTokenCookie, tokens.RefreshToken, CreateCookieOptions(tokens.RefreshTokenExpiresAt));
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookie, CreateCookieOptions(DateTimeOffset.UnixEpoch));
    }

    private CookieOptions CreateCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Expires = expires,
        Path = "/api/auth"
    };

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static AuthenticationResponse ToResponse(AuthenticationTokens tokens) =>
        new(tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.User);
}
