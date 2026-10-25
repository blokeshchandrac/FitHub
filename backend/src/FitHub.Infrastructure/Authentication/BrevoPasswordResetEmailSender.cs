using System.Net.Http.Json;
using System.Text.Encodings.Web;
using FitHub.Application.Authentication;
using Microsoft.Extensions.Options;

namespace FitHub.Infrastructure.Authentication;

/// <summary>Sends password-recovery mail through Brevo's HTTPS transactional-email API.</summary>
public sealed class BrevoPasswordResetEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options) : IPasswordResetEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        TimeSpan validity,
        CancellationToken cancellationToken = default)
    {
        var encoder = HtmlEncoder.Default;
        var minutes = Math.Max(1, (int)Math.Ceiling(validity.TotalMinutes));
        var request = new BrevoTransactionalEmail(
            new BrevoContact(_options.SenderAddress, _options.SenderName),
            [new BrevoContact(recipientEmail, recipientName)],
            "Reset your FitHub password",
            $"""
             <p>Hello {encoder.Encode(recipientName)},</p>
             <p>A password reset was requested for your FitHub account.</p>
             <p><a href="{encoder.Encode(resetUrl)}">Reset your password</a></p>
             <p>This single-use link expires in {minutes} minutes. If you did not request it, you can ignore this email.</p>
             """);

        using var message = new HttpRequestMessage(HttpMethod.Post, "/v3/smtp/email")
        {
            Content = JsonContent.Create(request)
        };
        // The API key is added only to this server-to-server request and is never part of the body or a log message.
        message.Headers.Add("api-key", _options.Brevo.ApiKey);
        message.Headers.Accept.ParseAdd("application/json");

        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "The transactional email provider rejected the request.",
                inner: null,
                response.StatusCode);
        }
    }

    private sealed record BrevoContact(string Email, string Name);
    private sealed record BrevoTransactionalEmail(
        BrevoContact Sender,
        IReadOnlyCollection<BrevoContact> To,
        string Subject,
        string HtmlContent);
}
