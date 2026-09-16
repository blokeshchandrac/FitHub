using FitHub.Domain.Common;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;

namespace FitHub.Domain.Payments;

public sealed class Payment : Entity
{
    private decimal _amount;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? MembershipId { get; set; }
    public Membership? Membership { get; set; }
    public Guid MembershipPlanId { get; set; }
    public MembershipPlan MembershipPlan { get; set; } = null!;
    public MembershipPurchaseKind PurchaseKind { get; set; }
    public decimal Amount
    {
        get => _amount;
        set => _amount = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ProviderReference { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public decimal RefundedAmount { get; private set; }
    public DateTimeOffset? RefundedAt { get; private set; }
    public bool IsSeededDemo { get; set; }
    public string? FailureReason { get; set; }

    public void MarkSucceeded(DateTimeOffset paidAt, string? stripePaymentIntentId = null)
    {
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt;
        if (!string.IsNullOrWhiteSpace(stripePaymentIntentId)) StripePaymentIntentId = stripePaymentIntentId;
        FailureReason = null;
    }

    public void RecordRefund(decimal refundedAmount, DateTimeOffset refundedAt)
    {
        if (refundedAmount <= 0 || refundedAmount > Amount)
            throw new ArgumentOutOfRangeException(nameof(refundedAmount));
        if (refundedAmount <= RefundedAmount) return;
        RefundedAmount = decimal.Round(refundedAmount, 2);
        RefundedAt = refundedAt;
        if (RefundedAmount == Amount) Status = PaymentStatus.Refunded;
    }

    public void MarkFailed(string? reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Payment was not completed." : reason;
    }

    public void MarkCancelled(string? reason)
    {
        Status = PaymentStatus.Cancelled;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Checkout was cancelled or expired." : reason;
    }
}
