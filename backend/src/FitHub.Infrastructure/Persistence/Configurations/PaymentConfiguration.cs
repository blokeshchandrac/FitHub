using FitHub.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : EntityConfiguration<Payment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.PurchaseKind).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.ProviderReference).HasMaxLength(200);
        builder.Property(payment => payment.FailureReason).HasMaxLength(500);
        builder.HasOne(payment => payment.User).WithMany().HasForeignKey(payment => payment.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(payment => payment.Membership).WithMany().HasForeignKey(payment => payment.MembershipId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(payment => payment.MembershipPlan).WithMany().HasForeignKey(payment => payment.MembershipPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(payment => new { payment.UserId, payment.CreatedAt });
        builder.HasIndex(payment => payment.MembershipId);
        builder.HasIndex(payment => payment.MembershipPlanId);
        builder.HasIndex(payment => payment.ProviderReference).IsUnique();
    }
}
