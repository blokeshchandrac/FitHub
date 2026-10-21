using FitHub.Application.Common;
using FitHub.Application.Payments;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Payments;

internal sealed class PaymentReconciliationService(
    FitHubDbContext dbContext,
    IPaymentCheckoutVerifier checkoutVerifier,
    TimeProvider timeProvider) : IPaymentReconciliationService
{
    public async Task<PaymentReconciliationResult> ReconcileAsync(
        Guid userId,
        string? checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        if (checkoutSessionId is not null)
        {
            ValidateSessionId(checkoutSessionId);
        }

        var resolvedSessionId = checkoutSessionId ?? await dbContext.Payments.AsNoTracking()
            .Where(payment =>
                payment.UserId == userId &&
                payment.Status == PaymentStatus.Pending &&
                payment.ProviderReference != null)
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => payment.ProviderReference)
            .FirstOrDefaultAsync(cancellationToken);
        if (resolvedSessionId is null)
        {
            return new PaymentReconciliationResult(false, false, "No pending checkout is available.");
        }

        var paymentExists = await dbContext.Payments.AsNoTracking().AnyAsync(
            payment => payment.UserId == userId && payment.ProviderReference == resolvedSessionId,
            cancellationToken);
        if (!paymentExists)
        {
            throw Validation("sessionId", "The checkout session is not available for this account.");
        }

        var verification = await checkoutVerifier.VerifyAsync(resolvedSessionId, cancellationToken);
        if (!verification.IsPaid || !verification.IsComplete)
        {
            return new PaymentReconciliationResult(false, false, "Stripe has not confirmed the payment.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var payment = await dbContext.Payments
            .Include(item => item.Membership)
            .Include(item => item.MembershipPlan)
            .SingleAsync(
                item => item.UserId == userId && item.ProviderReference == resolvedSessionId,
                cancellationToken);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            await transaction.CommitAsync(cancellationToken);
            return new PaymentReconciliationResult(true, false, "Payment was already confirmed.");
        }

        ValidateSuccessfulCheckout(payment, verification);
        var now = timeProvider.GetUtcNow();
        payment.MarkSucceeded(now, verification.StripePaymentIntentId);
        PaymentMembershipActivator.Apply(payment, now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PaymentReconciliationResult(true, true, "Payment confirmed.");
    }

    private static void ValidateSessionId(string checkoutSessionId)
    {
        if (string.IsNullOrWhiteSpace(checkoutSessionId) ||
            checkoutSessionId.Length > 255 ||
            !checkoutSessionId.StartsWith("cs_test_", StringComparison.Ordinal))
        {
            throw Validation("sessionId", "A valid Stripe Test Mode checkout session is required.");
        }
    }

    private static void ValidateSuccessfulCheckout(
        Payment payment,
        PaymentCheckoutVerification verification)
    {
        var expectedAmountMinor = PaymentAmount.ToMinorUnits(payment.Amount);
        if (verification.IsLiveMode ||
            verification.CheckoutSessionId != payment.ProviderReference ||
            verification.PaymentId != payment.Id ||
            verification.UserId != payment.UserId ||
            verification.PlanId != payment.MembershipPlanId ||
            verification.PurchaseKind != payment.PurchaseKind ||
            verification.AmountTotalMinor != expectedAmountMinor ||
            !string.Equals(verification.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentCheckoutVerificationException(
                "Stripe Checkout does not match the pending FitHub payment.");
        }
    }

    private static CommandValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
