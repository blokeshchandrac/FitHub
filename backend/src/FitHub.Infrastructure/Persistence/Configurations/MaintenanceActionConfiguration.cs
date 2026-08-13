using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceActionConfiguration : EntityConfiguration<MaintenanceAction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MaintenanceAction> builder)
    {
        builder.Property(action => action.ActionType).HasConversion<string>().HasMaxLength(30);
        builder.Property(action => action.Description).HasMaxLength(4000).IsRequired();
        builder.Property(action => action.Cost).HasPrecision(12, 2);
        builder.HasOne(action => action.Equipment).WithMany().HasForeignKey(action => action.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(action => action.FaultReport).WithMany(report => report.MaintenanceActions).HasForeignKey(action => action.FaultReportId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(action => action.PerformedByUser).WithMany().HasForeignKey(action => action.PerformedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(action => new { action.EquipmentId, action.PerformedAt });
        builder.HasIndex(action => action.FaultReportId);
        builder.HasIndex(action => action.PerformedByUserId);
    }
}
