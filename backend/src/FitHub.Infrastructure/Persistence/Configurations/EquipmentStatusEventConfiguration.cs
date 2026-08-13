using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class EquipmentStatusEventConfiguration : EntityConfiguration<EquipmentStatusEvent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EquipmentStatusEvent> builder)
    {
        builder.Property(statusEvent => statusEvent.State).HasConversion<string>().HasMaxLength(30);
        builder.Property(statusEvent => statusEvent.PreviousState).HasConversion<string>().HasMaxLength(30);
        builder.Property(statusEvent => statusEvent.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property(statusEvent => statusEvent.Note).HasMaxLength(500);
        builder.HasOne(statusEvent => statusEvent.Equipment).WithMany(equipment => equipment.StatusHistory).HasForeignKey(statusEvent => statusEvent.EquipmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(statusEvent => statusEvent.ChangedByUser).WithMany().HasForeignKey(statusEvent => statusEvent.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(statusEvent => new { statusEvent.EquipmentId, statusEvent.OccurredAt });
        builder.HasIndex(statusEvent => statusEvent.ChangedByUserId);
    }
}
