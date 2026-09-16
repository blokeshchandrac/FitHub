using FitHub.Domain.Parking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FitHub.Infrastructure.Persistence.Configurations;
public sealed class ParkingStatusEventConfiguration : EntityConfiguration<ParkingStatusEvent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ParkingStatusEvent> builder)
    {
        builder.Property(x=>x.PreviousState).HasConversion<string>().HasMaxLength(40);
        builder.Property(x=>x.State).HasConversion<string>().HasMaxLength(40);
        builder.Property(x=>x.Confidence).HasPrecision(5,4);
        builder.Property(x=>x.Source).HasMaxLength(100).IsRequired();
        builder.Property(x=>x.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property(x=>x.Reason).HasMaxLength(500);
        builder.HasOne(x=>x.ParkingSpace).WithMany(x=>x.StatusHistory).HasForeignKey(x=>x.ParkingSpaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x=>x.ChangedByUser).WithMany().HasForeignKey(x=>x.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x=>new{x.ParkingSpaceId,x.OccurredAt});builder.HasIndex(x=>x.ChangedByUserId);
    }
}
