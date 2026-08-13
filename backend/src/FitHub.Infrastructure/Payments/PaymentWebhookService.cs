using FitHub.Application.Payments;
using FitHub.Domain.Memberships;
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
        if (await dbContext.ProcessedPaymentWebhooks.AnyAsync(
                item => item.Provider == "Stripe" && item.EventId == notification.EventId, cancellationToken))
        {
            return new PaymentWebhookResult(false, true, "Webhook was already processed.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.ProcessedPaymentWebhooks.Add(new ProcessedPaymentWebhook(
            "Stripe", notification.EventId, notification.EventType, timeProvider.GetUtcNow()));

        var payment = string.IsNullOrWhiteSpace(notification.CheckoutSessionId) && notification.PaymentId is null
            ? null
            : await dbContext.Payments
                .Include(item => item.Membership)
                .Include(item => item.MembershipPlan)
                .SingleOrDefaultAsync(item => item.ProviderReference == notification.CheckoutSessionId || item.Id == notification.PaymentId, cancellationToken);

        if (payment is not null)
        {
            payment.ProviderReference ??= notification.CheckoutSessionId;
            if (notification.Outcome == PaymentWebhookOutcome.Succeeded && payment.Status != PaymentStatus.Succeeded)
            {
                ValidateSuccessfulCheckout(payment, notification);
                var now = timeProvider.GetUtcNow();
                payment.MarkSucceeded(now);
                ApplyMembership(payment, now);
            }
            else if (notification.Outcome == PaymentWebhookOutcome.Failed && payment.Status != PaymentStatus.Succeeded)
            {
                payment.MarkFailed("Stripe reported that payment was not completed.");
            }
            else if (notification.Outcome == PaymentWebhookOutcome.Cancelled && payment.Status != PaymentStatus.Succeeded)
            {
                payment.MarkCancelled("Stripe Checkout expired before payment was completed.");
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

    private static void ApplyMembership(Payment payment, DateTimeOffset now)
    {
        var membership = payment.Membership ?? throw new InvalidOperationException("Payment has no membership.");
        var plan = payment.MembershipPlan;
        switch (payment.PurchaseKind)
        {
            case MembershipPurchaseKind.Purchase:
                membership.Activate(now, now.AddMonths(plan.BillingPeriodMonths));
                break;
            case MembershipPurchaseKind.Renewal:
                var renewalStart = membership.EndsAt > now ? membership.EndsAt : now;
                membership.Renew(renewalStart.AddMonths(plan.BillingPeriodMonths));
                break;
            case MembershipPurchaseKind.Upgrade:
                membership.Upgrade(plan.Id, now, now.AddMonths(plan.BillingPeriodMonths));
                break;
            default:
                throw new InvalidOperationException("Unsupported membership payment type.");
        }
    }

    private static void ValidateSuccessfulCheckout(Payment payment, PaymentWebhookNotification notification)
    {
        if (notification.UserId != payment.UserId ||
            notification.PlanId != payment.MembershipPlanId ||
            notification.PurchaseKind != payment.PurchaseKind)
        {
            throw new PaymentWebhookValidationException("Stripe Checkout metadata does not match the pending payment.");
        }
    }
}
