namespace FitHub.Infrastructure.Authentication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string SmtpHost { get; init; } = "localhost";
    public int SmtpPort { get; init; } = 1025;
    public bool UseTls { get; init; }
    public string SenderAddress { get; init; } = "no-reply@fithub.local";
    public string SenderName { get; init; } = "FitHub";
}
