using FitHub.Application.Authentication;

namespace FitHub.Api.Models.Authentication;

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User);
