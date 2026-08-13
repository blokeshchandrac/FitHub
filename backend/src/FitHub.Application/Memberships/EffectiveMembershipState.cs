namespace FitHub.Application.Memberships;

public enum EffectiveMembershipState
{
    None,
    PendingPayment,
    Active,
    Expired,
    Cancelled,
    PastDue
}
