namespace FitHub.Api.Models.Memberships;

public sealed record UpsertMembershipPlanRequest(string Name, string? Description, decimal Price, int BillingPeriodMonths);
