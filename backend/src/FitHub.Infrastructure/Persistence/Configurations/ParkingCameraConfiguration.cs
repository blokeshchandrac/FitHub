using FitHub.Domain.Parking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FitHub.Infrastructure.Persistence.Configurations;
public sealed class ParkingCameraConfiguration : EntityConfiguration<ParkingCamera>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ParkingCamera> builder){builder.Property(x=>x.CameraCode).HasMaxLength(80).IsRequired();builder.Property(x=>x.CredentialHash).HasMaxLength(500).IsRequired();builder.HasIndex(x=>x.CameraCode).IsUnique();}
}
