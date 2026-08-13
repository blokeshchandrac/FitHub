using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FitHub.Infrastructure.Persistence.Configurations;
public sealed class EquipmentDeviceConfiguration : EntityConfiguration<EquipmentDevice>
{
    protected override void ConfigureEntity(EntityTypeBuilder<EquipmentDevice> builder)
    {
        builder.Property(x => x.DeviceCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CredentialHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.DeviceCode).IsUnique();
        builder.HasIndex(x => x.EquipmentId).IsUnique();
        builder.HasOne(x => x.Equipment).WithOne().HasForeignKey<EquipmentDevice>(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
