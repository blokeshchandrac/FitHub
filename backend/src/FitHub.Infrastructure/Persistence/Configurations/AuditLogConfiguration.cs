using FitHub.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : EntityConfiguration<AuditLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(log => log.Action).HasMaxLength(120).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(log => log.Details).HasMaxLength(8000);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.HasOne(log => log.User).WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(log => log.OccurredAt);
        builder.HasIndex(log => new { log.EntityType, log.EntityId });
        builder.HasIndex(log => log.UserId);
    }
}
