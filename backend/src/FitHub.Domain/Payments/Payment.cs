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
    public DateTimeOffset? PaidAt { get; set; }
    public string? FailureReason { get; set; }

    public void MarkSucceeded(DateTimeOffset paidAt)
    {
        Status = PaymentStatus.Succeeded;
        PaidAt = paidAt;
        FailureReason = null;
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
