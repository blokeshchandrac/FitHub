using FitHub.Domain.Payments;

namespace FitHub.Application.Payments;

public interface IPaymentCheckoutVerifier
{
    Task<PaymentCheckoutVerification> VerifyAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentCheckoutVerification(
    string CheckoutSessionId,
    Guid? PaymentId,
    Guid? UserId,
    Guid? PlanId,
    MembershipPurchaseKind? PurchaseKind,
    bool IsPaid,
    bool IsComplete,
    bool IsLiveMode,
    long? AmountTotalMinor,
    string? Currency,
    string? StripePaymentIntentId = null);

public sealed class PaymentCheckoutVerificationException : Exception
{
    public PaymentCheckoutVerificationException(string message) : base(message) { }

    public PaymentCheckoutVerificationException(string message, Exception innerException)
        : base(message, innerException) { }
}
