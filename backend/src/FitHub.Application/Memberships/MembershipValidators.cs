using FitHub.Application.Common;

namespace FitHub.Application.Memberships;

public static class MembershipValidators
{
    public static void Validate(CreateCheckoutCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.PlanId == Guid.Empty) errors["planId"] = ["A membership plan is required."];
        if (!Enum.IsDefined(command.PurchaseKind)) errors["purchaseKind"] = ["Purchase type is invalid."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }

    public static void Validate(UpsertMembershipPlanCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 120) errors["name"] = ["Name is required and must not exceed 120 characters."];
        if (command.Description?.Length > 1000) errors["description"] = ["Description must not exceed 1000 characters."];
        if (command.Price is <= 0 or > 9999999999.99m || decimal.Round(command.Price, 2) != command.Price) errors["price"] = ["Price must be positive with at most two decimal places."];
        if (command.BillingPeriodMonths is < 1 or > 36) errors["billingPeriodMonths"] = ["Billing period must be between 1 and 36 months."];
        if (errors.Count > 0) throw new CommandValidationException(errors);
    }
}
