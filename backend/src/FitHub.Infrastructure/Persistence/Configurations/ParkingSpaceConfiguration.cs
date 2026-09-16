using FitHub.Domain.Parking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ParkingSpaceConfiguration : EntityConfiguration<ParkingSpace>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ParkingSpace> builder)
    {
        builder.Property(space => space.Code).HasMaxLength(50).IsRequired();
        builder.Property(space => space.Zone).HasMaxLength(100).IsRequired();
        builder.Property(space => space.CurrentState).HasConversion<string>().HasMaxLength(40);
        builder.Property(space => space.CurrentConfidence).HasPrecision(5, 4);
        builder.Property(space => space.CurrentSource).HasMaxLength(100).IsRequired();
        builder.HasIndex(space => space.Code).IsUnique();
        builder.HasIndex(space => new { space.Zone, space.CurrentState });
        builder.HasIndex(space => space.IsAccessible);
    }
}
