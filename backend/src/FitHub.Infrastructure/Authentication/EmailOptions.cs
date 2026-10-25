namespace FitHub.Infrastructure.Authentication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public const string SmtpProvider = "Smtp";
    public const string BrevoProvider = "Brevo";

    public string Provider { get; init; } = SmtpProvider;
    public string SmtpHost { get; init; } = "localhost";
    public int SmtpPort { get; init; } = 1025;
    public bool UseTls { get; init; }
    public string SenderAddress { get; init; } = "no-reply@fithub.local";
    public string SenderName { get; init; } = "FitHub";
    public BrevoEmailOptions Brevo { get; init; } = new();
}

public sealed class BrevoEmailOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.brevo.com";
    public int TimeoutSeconds { get; init; } = 15;
}
