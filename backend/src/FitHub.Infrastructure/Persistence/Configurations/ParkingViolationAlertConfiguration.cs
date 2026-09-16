using FitHub.Domain.Parking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ParkingViolationAlertConfiguration : EntityConfiguration<ParkingViolationAlert>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ParkingViolationAlert> builder)
    {
        builder.Property(x => x.SimulationSessionId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.VehicleSimulationId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AffectedSpaceCodes).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ViolationType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceDeviceId).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(240).IsRequired();
        builder.HasIndex(x => x.DeduplicationKey).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.DetectedAt });
        builder.HasIndex(x => x.SimulationSessionId);
    }
}
