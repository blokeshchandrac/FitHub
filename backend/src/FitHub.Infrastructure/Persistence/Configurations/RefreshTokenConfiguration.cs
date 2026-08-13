using FitHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : EntityConfiguration<RefreshToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.CreatedByIp).HasMaxLength(64);
        builder.Property(token => token.RevokedByIp).HasMaxLength(64);
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
        builder.Ignore(token => token.IsActive);
        builder.HasOne(token => token.User).WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });
        builder.HasIndex(token => token.ReplacedByTokenHash);
    }
}
