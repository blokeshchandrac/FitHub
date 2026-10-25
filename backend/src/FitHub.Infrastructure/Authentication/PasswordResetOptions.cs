namespace FitHub.Infrastructure.Authentication;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public int ExpiryMinutes { get; init; } = 30;
    public string FrontendBaseUrl { get; init; } = "http://localhost:4040";
}
