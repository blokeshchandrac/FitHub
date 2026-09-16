using FitHub.Application.Payments;
using FitHub.Domain.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FitHub.Infrastructure.Payments;

internal sealed class StripeCheckoutVerifier(IOptions<StripeOptions> options) : IPaymentCheckoutVerifier
{
    private readonly StripeOptions _options = options.Value;

    public async Task<PaymentCheckoutVerification> VerifyAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        if (!StripeOptions.IsTestSecretKey(_options.SecretKey))
        {
            throw new PaymentCheckoutVerificationException("Stripe Test Mode configuration is invalid.");
        }

        Session session;
        try
        {
            var client = new StripeClient(_options.SecretKey);
            session = await new SessionService(client).GetAsync(
                checkoutSessionId,
                cancellationToken: cancellationToken);
        }
        catch (StripeException exception)
        {
            throw new PaymentCheckoutVerificationException(
                "Stripe Checkout could not be verified.", exception);
        }

        var paymentReference = session.ClientReferenceId;
        if (string.IsNullOrWhiteSpace(paymentReference) &&
            session.Metadata.TryGetValue("payment_id", out var metadataPaymentId))
        {
            paymentReference = metadataPaymentId;
        }

        Guid? paymentId = Guid.TryParse(paymentReference, out var parsedPaymentId) ? parsedPaymentId : null;
        Guid? userId = TryGetGuid(session, "user_id");
        Guid? planId = TryGetGuid(session, "plan_id");
        MembershipPurchaseKind? purchaseKind =
            session.Metadata.TryGetValue("purchase_kind", out var kind) &&
            Enum.TryParse<MembershipPurchaseKind>(kind, false, out var parsedKind)
                ? parsedKind
                : null;

        return new PaymentCheckoutVerification(
            session.Id,
            paymentId,
            userId,
            planId,
            purchaseKind,
            string.Equals(session.PaymentStatus, "paid", StringComparison.Ordinal),
            string.Equals(session.Status, "complete", StringComparison.Ordinal),
            session.Livemode,
            session.AmountTotal,
            session.Currency,
            session.PaymentIntentId);
    }

    private static Guid? TryGetGuid(Session session, string key) =>
        session.Metadata.TryGetValue(key, out var value) && Guid.TryParse(value, out var parsed)
            ? parsed
            : null;
}
