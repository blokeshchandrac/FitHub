namespace FitHub.Application.Authentication;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        TimeSpan validity,
        CancellationToken cancellationToken = default);
}
