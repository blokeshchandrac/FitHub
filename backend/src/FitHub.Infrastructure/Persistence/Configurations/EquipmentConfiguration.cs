using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class EquipmentConfiguration : EntityConfiguration<Equipment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Equipment> builder)
    {
        builder.Property(equipment => equipment.Code).HasMaxLength(50).IsRequired();
        builder.Property(equipment => equipment.Name).HasMaxLength(150).IsRequired();
        builder.Property(equipment => equipment.Description).HasMaxLength(1000);
        builder.Property(equipment => equipment.Location).HasMaxLength(150);
        builder.Property(equipment => equipment.CurrentState).HasConversion<string>().HasMaxLength(30);
        builder.Property(equipment => equipment.StateBeforeOffline).HasConversion<string>().HasMaxLength(30);
        builder.Property(equipment => equipment.Version).IsRowVersion().HasColumnName("xmin");
        builder.HasOne<FitHub.Domain.Identity.ApplicationUser>().WithMany().HasForeignKey(equipment => equipment.OwnerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<FitHub.Domain.Identity.ApplicationUser>().WithMany().HasForeignKey(equipment => equipment.PartnerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(equipment => equipment.Code).IsUnique();
        builder.HasIndex(equipment => equipment.CurrentState);
        builder.HasIndex(equipment => equipment.Location);
        builder.HasIndex(equipment => equipment.OwnerUserId);
    }
}
