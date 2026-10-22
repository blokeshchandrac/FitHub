using System.Net;
using System.Text.Json;
using FitHub.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace FitHub.IntegrationTests;

public sealed class BrevoPasswordResetEmailSenderTests
{
    [Fact]
    public async Task SendsExpectedTransactionalRequestWithoutPuttingApiKeyInPayload()
    {
        const string apiKey = "test-only-brevo-api-key";
        var handler = new RecordingHandler(HttpStatusCode.Created);
        var sender = CreateSender(handler, apiKey);

        await sender.SendPasswordResetAsync(
            "member@fithub.test",
            "Test Member",
            "https://fithub-web-10252426.onrender.com/reset-password?token=safe-test-token",
            TimeSpan.FromMinutes(30));

        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.RequestUri?.ToString());
        Assert.Equal(apiKey, Assert.Single(handler.Headers["api-key"]));
        Assert.DoesNotContain(apiKey, handler.Body, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(handler.Body);
        Assert.Equal("no-reply@fithub.test", json.RootElement.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("member@fithub.test", json.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Contains("https://fithub-web-10252426.onrender.com/reset-password", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProviderFailureUsesSafeMessageWithoutLeakingApiKey()
    {
        const string apiKey = "test-only-brevo-api-key";
        var sender = CreateSender(new RecordingHandler(HttpStatusCode.Unauthorized), apiKey);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => sender.SendPasswordResetAsync(
            "member@fithub.test", "Test Member", "https://example.test/reset", TimeSpan.FromMinutes(30)));

        Assert.DoesNotContain(apiKey, exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("transactional email provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static BrevoPasswordResetEmailSender CreateSender(HttpMessageHandler handler, string apiKey)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.com") };
        return new BrevoPasswordResetEmailSender(client, Options.Create(new EmailOptions
        {
            Provider = EmailOptions.BrevoProvider,
            SenderAddress = "no-reply@fithub.test",
            SenderName = "FitHub",
            Brevo = new BrevoEmailOptions { ApiKey = apiKey }
        }));
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public Dictionary<string, string[]> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            foreach (var header in request.Headers)
                Headers[header.Key] = header.Value.ToArray();
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }
}
