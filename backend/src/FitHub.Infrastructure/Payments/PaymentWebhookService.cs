using FitHub.Application.Payments;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Payments;

internal sealed class PaymentWebhookService(
    FitHubDbContext dbContext,
    IPaymentGateway paymentGateway,
    TimeProvider timeProvider) : IPaymentWebhookService
{
    public async Task<PaymentWebhookResult> ProcessAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        var notification = paymentGateway.ParseWebhook(payload, signature);
        // Stripe retries delivery. Persisting its event identifier makes membership activation
        // idempotent even when the same successful event arrives concurrently or more than once.
        if (await dbContext.ProcessedPaymentWebhooks.AnyAsync(
                item => item.Provider == "Stripe" && item.EventId == notification.EventId, cancellationToken))
        {
            return new PaymentWebhookResult(false, true, "Webhook was already processed.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.ProcessedPaymentWebhooks.Add(new ProcessedPaymentWebhook(
            "Stripe", notification.EventId, notification.EventType, timeProvider.GetUtcNow()));

        var payment = string.IsNullOrWhiteSpace(notification.CheckoutSessionId) && notification.PaymentId is null && string.IsNullOrWhiteSpace(notification.StripePaymentIntentId)
            ? null
            : await dbContext.Payments
                .Include(item => item.Membership)
                .Include(item => item.MembershipPlan)
                .SingleOrDefaultAsync(item =>
                    (!string.IsNullOrWhiteSpace(notification.CheckoutSessionId) && item.ProviderReference == notification.CheckoutSessionId) ||
                    (notification.PaymentId.HasValue && item.Id == notification.PaymentId.Value) ||
                    (!string.IsNullOrWhiteSpace(notification.StripePaymentIntentId) && item.StripePaymentIntentId == notification.StripePaymentIntentId),
                    cancellationToken);

        if (payment is not null)
        {
            payment.ProviderReference ??= notification.CheckoutSessionId;
            if (notification.Outcome == PaymentWebhookOutcome.Succeeded && payment.Status == PaymentStatus.Pending)
            {
                ValidateSuccessfulCheckout(payment, notification);
                var now = timeProvider.GetUtcNow();
                payment.MarkSucceeded(now, notification.StripePaymentIntentId);
                PaymentMembershipActivator.Apply(payment, now);
            }
            else if (notification.Outcome == PaymentWebhookOutcome.Failed && payment.Status == PaymentStatus.Pending)
            {
                payment.MarkFailed("Stripe reported that payment was not completed.");
            }
            else if (notification.Outcome == PaymentWebhookOutcome.Cancelled && payment.Status == PaymentStatus.Pending)
            {
                payment.MarkCancelled("Stripe Checkout expired before payment was completed.");
            }
            else if (notification.Outcome == PaymentWebhookOutcome.Refunded &&
                     (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Refunded) &&
                     notification.RefundedAmountMinor is > 0)
            {
                if (!string.Equals(notification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
                    throw new PaymentWebhookValidationException("Stripe refund currency does not match the payment.");
                payment.RecordRefund(notification.RefundedAmountMinor.Value / 100m, timeProvider.GetUtcNow());
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PaymentWebhookResult(true, false, payment is null ? "Webhook recorded." : "Payment updated.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            if (await dbContext.ProcessedPaymentWebhooks.AnyAsync(
                    item => item.Provider == "Stripe" && item.EventId == notification.EventId, cancellationToken))
            {
                return new PaymentWebhookResult(false, true, "Webhook was already processed.");
            }

            throw;
        }
    }

    private static void ValidateSuccessfulCheckout(Payment payment, PaymentWebhookNotification notification)
    {
        if (notification.UserId != payment.UserId ||
            notification.PlanId != payment.MembershipPlanId ||
            notification.PurchaseKind != payment.PurchaseKind ||
            notification.IsLiveMode ||
            notification.AmountTotalMinor != PaymentAmount.ToMinorUnits(payment.Amount) ||
            !string.Equals(notification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentWebhookValidationException("Stripe Checkout metadata does not match the pending payment.");
        }
    }
}
