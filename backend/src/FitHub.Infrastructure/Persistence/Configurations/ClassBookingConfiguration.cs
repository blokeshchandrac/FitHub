using FitHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ClassBookingConfiguration : EntityConfiguration<ClassBooking>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClassBooking> builder)
    {
        builder.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(booking => booking.ClassSchedule).WithMany(schedule => schedule.Bookings).HasForeignKey(booking => booking.ClassScheduleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(booking => booking.User).WithMany().HasForeignKey(booking => booking.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(booking => new { booking.ClassScheduleId, booking.UserId }).IsUnique();
        builder.HasIndex(booking => new { booking.UserId, booking.Status });
    }
}
