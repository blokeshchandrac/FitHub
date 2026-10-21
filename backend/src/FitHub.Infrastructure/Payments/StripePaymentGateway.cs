using FitHub.Application.Payments;
using FitHub.Domain.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FitHub.Infrastructure.Payments;

internal sealed class StripePaymentGateway(IOptions<StripeOptions> options) : IPaymentGateway
{
    private readonly StripeOptions _options = options.Value;

    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSandboxConfiguration();
        // Stripe credentials remain server-side; the browser receives only the hosted Checkout URL.
        var client = new StripeClient(_options.SecretKey);
        var service = new SessionService(client);
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = _options.CheckoutSuccessUrl,
            CancelUrl = _options.CheckoutCancelUrl,
            ClientReferenceId = request.PaymentId.ToString(),
            CustomerEmail = request.CustomerEmail,
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = request.PaymentId.ToString(),
                ["user_id"] = request.UserId.ToString(),
                ["plan_id"] = request.PlanId.ToString(),
                ["purchase_kind"] = request.PurchaseKind.ToString()
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = PaymentAmount.ToMinorUnits(request.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = request.PlanName }
                    }
                }
            ]
        }, cancellationToken: cancellationToken);

        return new PaymentCheckoutResult(session.Id, session.Url);
    }

    public PaymentWebhookNotification ParseWebhook(string payload, string signature)
    {
        EnsureSandboxConfiguration();
        if (!StripeOptions.IsWebhookSecret(_options.WebhookSecret))
        {
            throw new InvalidOperationException("Stripe Sandbox webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _options.WebhookSecret);
        }
        catch (StripeException)
        {
            throw new PaymentWebhookValidationException("Stripe webhook signature or payload is invalid.");
        }
        var session = stripeEvent.Data.Object as Session;
        var charge = stripeEvent.Data.Object as Charge;
        var outcome = stripeEvent.Type switch
        {
            "checkout.session.completed" when session?.PaymentStatus == "paid" => PaymentWebhookOutcome.Succeeded,
            "checkout.session.completed" => PaymentWebhookOutcome.Pending,
            "checkout.session.async_payment_succeeded" => PaymentWebhookOutcome.Succeeded,
            "checkout.session.async_payment_failed" => PaymentWebhookOutcome.Failed,
            "checkout.session.expired" => PaymentWebhookOutcome.Cancelled,
            "charge.refunded" => PaymentWebhookOutcome.Refunded,
            _ => PaymentWebhookOutcome.Ignored
        };
        var paymentReference = session?.ClientReferenceId;
        if (string.IsNullOrWhiteSpace(paymentReference) && session?.Metadata.TryGetValue("payment_id", out var metadataPaymentId) == true)
        {
            paymentReference = metadataPaymentId;
        }
        Guid? paymentId = Guid.TryParse(paymentReference, out var parsedPaymentId) ? parsedPaymentId : null;
        Guid? userId = TryGetGuid(session, "user_id");
        Guid? planId = TryGetGuid(session, "plan_id");
        MembershipPurchaseKind? purchaseKind = session?.Metadata.TryGetValue("purchase_kind", out var kind) == true &&
                                                Enum.TryParse<MembershipPurchaseKind>(kind, false, out var parsedKind)
            ? parsedKind
            : null;
        return new PaymentWebhookNotification(
            stripeEvent.Id, stripeEvent.Type, session?.Id ?? string.Empty,
            paymentId, userId, planId, purchaseKind, outcome,
            session?.PaymentIntentId ?? charge?.PaymentIntentId,
            charge?.AmountRefunded,
            session?.Currency ?? charge?.Currency,
            session?.AmountTotal,
            session?.Livemode ?? charge?.Livemode ?? false);
    }

    private void EnsureSandboxConfiguration()
    {
        if (!StripeOptions.IsTestSecretKey(_options.SecretKey))
        {
            throw new InvalidOperationException("Stripe Test Mode configuration is invalid.");
        }
    }

    private static Guid? TryGetGuid(Session? session, string key) =>
        session?.Metadata.TryGetValue(key, out var value) == true && Guid.TryParse(value, out var parsed)
            ? parsed
            : null;
}
