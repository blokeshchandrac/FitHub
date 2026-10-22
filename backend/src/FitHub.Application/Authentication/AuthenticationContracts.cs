namespace FitHub.Application.Authentication;

public static class RegistrationConsent
{
    public const string CurrentVersion = "1.0";
}

public sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName, bool ConsentAccepted);

public sealed record LoginCommand(string Email, string Password);

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword);

public enum ResetPasswordStatus
{
    Succeeded,
    InvalidOrExpiredToken,
    PasswordRejected
}

public sealed record ResetPasswordResult(ResetPasswordStatus Status, IReadOnlyCollection<string> Errors)
{
    public static ResetPasswordResult Success() => new(ResetPasswordStatus.Succeeded, []);
    public static ResetPasswordResult InvalidToken() => new(ResetPasswordStatus.InvalidOrExpiredToken, []);
    public static ResetPasswordResult Rejected(IEnumerable<string> errors) =>
        new(ResetPasswordStatus.PasswordRejected, errors.ToArray());
}

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
