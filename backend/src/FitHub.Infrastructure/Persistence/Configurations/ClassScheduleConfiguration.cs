using FitHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ClassScheduleConfiguration : EntityConfiguration<ClassSchedule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClassSchedule> builder)
    {
        builder.Property(schedule => schedule.Name).HasMaxLength(150).IsRequired();
        builder.Property(schedule => schedule.Description).HasMaxLength(2000);
        builder.Property(schedule => schedule.Location).HasMaxLength(150);
        builder.Property(schedule => schedule.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(schedule => schedule.ReviewReason).HasMaxLength(1000);
        builder.HasOne(schedule => schedule.InstructorUser).WithMany().HasForeignKey(schedule => schedule.InstructorUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(schedule => schedule.StartsAt);
        builder.HasIndex(schedule => schedule.InstructorUserId);
        builder.HasIndex(schedule => new { schedule.Status, schedule.StartsAt });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ClassSchedules_Capacity", "\"Capacity\" > 0");
            table.HasCheckConstraint("CK_ClassSchedules_TimeRange", "\"EndsAt\" > \"StartsAt\"");
        });
    }
}
