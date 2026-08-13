using FitHub.Domain.Common;

namespace FitHub.Domain.Memberships;

public sealed class MembershipPlan : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int BillingPeriodMonths { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public void Update(string name, string? description, decimal price, int billingPeriodMonths)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Plan name is required.", nameof(name));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (billingPeriodMonths is < 1 or > 36) throw new ArgumentOutOfRangeException(nameof(billingPeriodMonths));

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Price = decimal.Round(price, 2);
        BillingPeriodMonths = billingPeriodMonths;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
