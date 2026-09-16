namespace FitHub.Application.Authentication;

public sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName);

public sealed record LoginCommand(string Email, string Password);

public sealed record CurrentUserDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyCollection<string> Roles, bool OnboardingCompleted);

public sealed record AuthenticationTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    CurrentUserDto User);

public sealed record AuthenticationResult(bool Succeeded, AuthenticationTokens? Tokens = null)
{
    public static AuthenticationResult Success(AuthenticationTokens tokens) => new(true, tokens);
    public static AuthenticationResult Failed() => new(false);
}
