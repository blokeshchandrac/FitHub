using System.Net.Mail;
using System.Text.Encodings.Web;
using FitHub.Application.Authentication;
using Microsoft.Extensions.Options;

namespace FitHub.Infrastructure.Authentication;

internal sealed class SmtpPasswordResetEmailSender(IOptions<EmailOptions> options) : IPasswordResetEmailSender
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
        var safeName = encoder.Encode(recipientName);
        var safeUrl = encoder.Encode(resetUrl);
        var minutes = Math.Max(1, (int)Math.Ceiling(validity.TotalMinutes));

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderAddress, _options.SenderName),
            Subject = "Reset your FitHub password",
            Body = $"""
                <p>Hello {safeName},</p>
                <p>A password reset was requested for your FitHub account.</p>
                <p><a href="{safeUrl}">Reset your password</a></p>
                <p>This single-use link expires in {minutes} minutes. If you did not request it, you can ignore this email.</p>
                """,
            IsBodyHtml = true
        };
        message.To.Add(recipientEmail);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseTls,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
