using FitHub.Domain.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class AttendanceCheckInTokenConfiguration : EntityConfiguration<AttendanceCheckInToken>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AttendanceCheckInToken> builder)
    {
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(token => token.Version).IsRowVersion();
        builder.HasOne(token => token.User).WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });
    }
}
