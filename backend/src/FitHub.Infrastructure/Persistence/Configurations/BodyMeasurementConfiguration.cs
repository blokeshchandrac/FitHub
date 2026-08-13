using FitHub.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class BodyMeasurementConfiguration : EntityConfiguration<BodyMeasurement>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.Property(measurement => measurement.WeightKg).HasPrecision(8, 2);
        builder.Property(measurement => measurement.HeightCm).HasPrecision(6, 2);
        builder.Property(measurement => measurement.BodyFatPercentage).HasPrecision(5, 2);
        builder.Property(measurement => measurement.ChestCm).HasPrecision(7, 2);
        builder.Property(measurement => measurement.WaistCm).HasPrecision(7, 2);
        builder.Property(measurement => measurement.HipCm).HasPrecision(7, 2);
        builder.Property(measurement => measurement.Notes).HasMaxLength(1000);
        builder.HasOne(measurement => measurement.User).WithMany().HasForeignKey(measurement => measurement.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(measurement => new { measurement.UserId, measurement.RecordedAt });
    }
}
