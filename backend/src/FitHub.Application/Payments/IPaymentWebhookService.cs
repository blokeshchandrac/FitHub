namespace FitHub.Application.Payments;

public interface IPaymentWebhookService
{
    Task<PaymentWebhookResult> ProcessAsync(string payload, string signature, CancellationToken cancellationToken = default);
}

public sealed record PaymentWebhookResult(bool Processed, bool Duplicate, string Message);
