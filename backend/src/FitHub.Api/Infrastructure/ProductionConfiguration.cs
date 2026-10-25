using FitHub.Infrastructure.Authentication;

namespace FitHub.Api.Infrastructure;

internal static class ProductionConfiguration
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        Require(configuration, "ConnectionStrings:FitHubDatabase");
        Require(configuration, "Jwt:SigningKey");
        Require(configuration, "Jwt:Issuer");
        Require(configuration, "Jwt:Audience");
        Require(configuration, "PasswordReset:FrontendBaseUrl");
        if (!configuration.GetValue<bool>("Database:ApplyMigrations"))
            throw new InvalidOperationException("Database:ApplyMigrations must be enabled in Production.");

        var connectionString = configuration.GetConnectionString("FitHubDatabase")!;
        if (!connectionString.Contains("SSL Mode=Require", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("SSL Mode=VerifyFull", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("sslmode=require", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("sslmode=verify-full", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:FitHubDatabase must require TLS in Production.");
        }

        if (configuration["Jwt:SigningKey"]!.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters in Production.");

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one HTTPS origin in Production.");

        var provider = configuration[$"{EmailOptions.SectionName}:Provider"];
        if (!string.Equals(provider, EmailOptions.BrevoProvider, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email:Provider must be Brevo in Production.");
        Require(configuration, "Email:Brevo:ApiKey");
        Require(configuration, "Email:SenderAddress");

        if (configuration.GetValue<bool>("SeedData:Enabled"))
        {
            if (!string.Equals(configuration["SeedData:Mode"], "Evaluation", StringComparison.Ordinal))
                throw new InvalidOperationException("Production seed data requires SeedData:Mode=Evaluation.");
            Require(configuration, "SeedData:DemoPassword");
            Require(configuration, "SeedData:EquipmentDeviceCredential");
            Require(configuration, "SeedData:MemberCardUid");
            Require(configuration, "SeedData:ParkingCameraCredential");
            Require(configuration, "EquipmentDevices:CardUidPepper");
        }
    }

    private static void Require(IConfiguration configuration, string key)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
            throw new InvalidOperationException($"Required Production configuration '{key}' is missing.");
    }
}
