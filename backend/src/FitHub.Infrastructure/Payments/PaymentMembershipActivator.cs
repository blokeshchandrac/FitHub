using FitHub.Domain.Payments;

namespace FitHub.Infrastructure.Payments;

internal static class PaymentMembershipActivator
{
    public static void Apply(Payment payment, DateTimeOffset now)
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
}
