using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FitHub.Infrastructure.Persistence.Configurations;
public sealed class FaultReportConfiguration : EntityConfiguration<FaultReport>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FaultReport> builder)
    {
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.MaintenanceNotes).HasMaxLength(4000);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReportedByUser).WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EquipmentId, x.Status });
        builder.HasIndex(x => x.ReportedByUserId);
        builder.HasIndex(x => new { x.Status, x.Priority });
    }
}
