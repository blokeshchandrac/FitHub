using FitHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : EntityConfiguration<PasswordResetToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.Property(item => item.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.InvalidationReason).HasMaxLength(120);
        builder.Property(item => item.CreatedByIp).HasMaxLength(64);
        builder.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.ExpiresAtUtc });
    }
}
