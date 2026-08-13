using FitHub.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : EntityConfiguration<Membership>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Membership> builder)
    {
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(membership => membership.User).WithMany().HasForeignKey(membership => membership.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(membership => membership.MembershipPlan).WithMany().HasForeignKey(membership => membership.MembershipPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(membership => new { membership.UserId, membership.Status });
        builder.HasIndex(membership => membership.MembershipPlanId);
        builder.HasIndex(membership => membership.EndsAt);
    }
}
