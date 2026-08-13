using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FitHub.Application.Payments;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Payments;
using FitHub.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Stripe;

namespace FitHub.IntegrationTests;

public sealed class StripeGatewaySecurityTests
{
    private static string WebhookSecret => "whsec_" + "stage9testsecretlongenough";
    private static string TestSecretKey => "sk_" + "test_" + "stage9keylongenough123456789";
    private static string PlaceholderSecretKey => "sk_" + "test_" + "replace_with_stripe_sandbox_secret";
    private static string LiveSecretKey => "sk_" + "live_" + "must_never_be_accepted";
    private static string PreviewImpostorKey => "sk_" + "test_" + "local_preview_not_a_real_stripe_key_123456";

    [Fact]
    public void ValidSandboxSignatureIsAcceptedAndMapped()
    {
        var paymentId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_stage9_signed",
            @object = "event",
            created = timestamp,
            api_version = StripeConfiguration.ApiVersion,
            livemode = false,
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = "cs_test_stage9",
                    @object = "checkout.session",
                    client_reference_id = paymentId,
                    payment_status = "paid",
                    metadata = new Dictionary<string, string>
                    {
                        ["user_id"] = Guid.NewGuid().ToString(),
                        ["plan_id"] = Guid.NewGuid().ToString(),
                        ["purchase_kind"] = "Purchase"
                    }
                }
            }
        });
        var gateway = CreateGateway(TestSecretKey, WebhookSecret);

        var result = gateway.ParseWebhook(payload, Sign(payload, timestamp, WebhookSecret));

        Assert.Equal("evt_stage9_signed", result.EventId);
        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(PaymentWebhookOutcome.Succeeded, result.Outcome);
        Assert.Equal(MembershipPurchaseKind.Purchase, result.PurchaseKind);
    }

    [Fact]
    public void TamperedPayloadIsRejectedByStripeSignatureVerification()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string original = "{\"id\":\"evt_original\",\"object\":\"event\",\"type\":\"ignored\",\"data\":{\"object\":{}}}";
        const string tampered = "{\"id\":\"evt_tampered\",\"object\":\"event\",\"type\":\"ignored\",\"data\":{\"object\":{}}}";
        var gateway = CreateGateway(TestSecretKey, WebhookSecret);

        Assert.Throws<PaymentWebhookValidationException>(() =>
            gateway.ParseWebhook(tampered, Sign(original, timestamp, WebhookSecret)));
    }

    [Fact]
    public void LiveStripeKeyIsRejected()
    {
        var gateway = CreateGateway(LiveSecretKey, WebhookSecret);

        Assert.Throws<InvalidOperationException>(() => gateway.ParseWebhook("{}", "invalid"));
    }

    [Fact]
    public void MissingPlaceholderOrLiveSecretKeyFailsDeveloperValidation()
    {
        Assert.False(StripeOptions.IsTestSecretKey(string.Empty));
        Assert.False(StripeOptions.IsTestSecretKey(PlaceholderSecretKey));
        Assert.False(StripeOptions.IsTestSecretKey(LiveSecretKey));
        Assert.False(StripeOptions.IsTestSecretKey(PreviewImpostorKey));
    }

    [Fact]
    public void ApplicationStartupClearlyRejectsMissingStripeConfiguration()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:FitHubDatabase", "Host=localhost;Database=unused;Username=unused;Password=unused");
            builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long");
            builder.UseSetting("SeedData:Enabled", "false");
            builder.UseSetting("Stripe:SecretKey", PlaceholderSecretKey);
            builder.UseSetting("Stripe:WebhookSecret", "whsec_replace_with_local_webhook_secret");
            builder.UseSetting("Stripe:WebBaseUrl", "http://localhost:4200");
            builder.UseSetting("Stripe:CheckoutSuccessUrl", "http://localhost:4200/membership/checkout/success?session_id={CHECKOUT_SESSION_ID}");
            builder.UseSetting("Stripe:CheckoutCancelUrl", "http://localhost:4200/membership/checkout/cancelled");
        });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("Stripe:SecretKey", exception.Message, StringComparison.Ordinal);
    }

    private static IPaymentGateway CreateGateway(string secretKey, string webhookSecret)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:FitHubDatabase"] = "Host=localhost;Database=unused",
            ["Jwt:SigningKey"] = "FitHub-stage9-tests-only-signing-key-at-least-32-characters",
            ["Stripe:SecretKey"] = secretKey,
            ["Stripe:WebhookSecret"] = webhookSecret
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider().GetRequiredService<IPaymentGateway>();
    }

    private static string Sign(string payload, long timestamp, string secret)
    {
        var signedPayload = $"{timestamp}.{payload}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(signedPayload));
        return $"t={timestamp},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
