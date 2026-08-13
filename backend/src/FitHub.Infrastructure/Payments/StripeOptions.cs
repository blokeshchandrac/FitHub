using System.Text.RegularExpressions;

namespace FitHub.Infrastructure.Payments;

public sealed class StripeOptions
{
    private static readonly Regex TestSecretKeyPattern = new(
        "^sk_test_[A-Za-z0-9]{24,}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex WebhookSecretPattern = new(
        "^whsec_[A-Za-z0-9]{24,}$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    public const string SectionName = "Stripe";
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string WebBaseUrl { get; init; } = string.Empty;
    public string CheckoutSuccessUrl { get; init; } = string.Empty;
    public string CheckoutCancelUrl { get; init; } = string.Empty;

    public static bool IsTestSecretKey(string value) =>
        TestSecretKeyPattern.IsMatch(value) &&
        !value.Contains("replace", StringComparison.OrdinalIgnoreCase);

    public static bool IsWebhookSecret(string value) =>
        WebhookSecretPattern.IsMatch(value) &&
        !value.Contains("replace", StringComparison.OrdinalIgnoreCase);

    public static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
