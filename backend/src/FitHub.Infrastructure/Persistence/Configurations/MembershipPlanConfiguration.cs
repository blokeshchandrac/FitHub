using FitHub.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class MembershipPlanConfiguration : EntityConfiguration<MembershipPlan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.Property(plan => plan.Name).HasMaxLength(120).IsRequired();
        builder.Property(plan => plan.Description).HasMaxLength(1000);
        builder.Property(plan => plan.Price).HasPrecision(12, 2);
        builder.HasIndex(plan => plan.Name).IsUnique();
        builder.HasIndex(plan => plan.IsActive);
    }
}
