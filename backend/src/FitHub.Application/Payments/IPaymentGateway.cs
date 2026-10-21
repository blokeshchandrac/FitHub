using FitHub.Domain.Payments;

namespace FitHub.Application.Payments;

public interface IPaymentGateway
{
    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default);
    PaymentWebhookNotification ParseWebhook(string payload, string signature);
}

public sealed record PaymentCheckoutRequest(
    Guid PaymentId,
    Guid UserId,
    Guid PlanId,
    MembershipPurchaseKind PurchaseKind,
    string? CustomerEmail,
    string PlanName,
    decimal Amount,
    string Currency);
public sealed record PaymentCheckoutResult(string SessionId, string CheckoutUrl);

public static class PaymentAmount
{
    public static long ToMinorUnits(decimal amount) =>
        checked((long)(decimal.Round(amount, 2, MidpointRounding.AwayFromZero) * 100m));
}

public sealed record PaymentWebhookNotification(
    string EventId,
    string EventType,
    string CheckoutSessionId,
    Guid? PaymentId,
    Guid? UserId,
    Guid? PlanId,
    MembershipPurchaseKind? PurchaseKind,
    PaymentWebhookOutcome Outcome,
    string? StripePaymentIntentId = null,
    long? RefundedAmountMinor = null,
    string? Currency = null,
    long? AmountTotalMinor = null,
    bool IsLiveMode = false);

public enum PaymentWebhookOutcome
{
    Pending,
    Succeeded,
    Failed,
    Cancelled,
    Refunded,
    Ignored
}

public sealed class PaymentWebhookValidationException(string message) : Exception(message);
