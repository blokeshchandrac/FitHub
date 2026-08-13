using FitHub.Domain.Payments;

namespace FitHub.Api.Models.Memberships;

public sealed record CreateCheckoutRequest(Guid PlanId, MembershipPurchaseKind PurchaseKind);
