using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;

namespace FitHub.Application.Memberships;

public sealed record MembershipPlanDto(Guid Id, string Name, string? Description, decimal Price, string Currency, int BillingPeriodMonths, bool IsActive);
public sealed record MembershipDto(Guid Id, MembershipPlanDto Plan, MembershipStatus Status, DateTimeOffset StartsAt, DateTimeOffset EndsAt, bool IsActive);
public sealed record PaymentDto(Guid Id, decimal Amount, string Currency, PaymentStatus Status, MembershipPurchaseKind PurchaseKind, string PlanName, DateTimeOffset CreatedAt, DateTimeOffset? PaidAt);
public sealed record MembershipOverviewDto(
    EffectiveMembershipState EffectiveState,
    MembershipDto? CurrentMembership,
    IReadOnlyCollection<MembershipPlanDto> Plans,
    IReadOnlyCollection<PaymentDto> Payments);
public sealed record CreateCheckoutCommand(Guid PlanId, MembershipPurchaseKind PurchaseKind);
public sealed record CheckoutSessionDto(Guid PaymentId, string CheckoutUrl);
public sealed record UpsertMembershipPlanCommand(string Name, string? Description, decimal Price, int BillingPeriodMonths);
