namespace FitHub.Application.Payments;

public interface IPaymentReconciliationService
{
    Task<PaymentReconciliationResult> ReconcileAsync(
        Guid userId,
        string? checkoutSessionId,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentReconciliationResult(bool Confirmed, bool Changed, string Message);
