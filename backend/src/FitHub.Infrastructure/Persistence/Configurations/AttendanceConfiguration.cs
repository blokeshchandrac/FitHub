using FitHub.Domain.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class AttendanceConfiguration : EntityConfiguration<Attendance>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasOne(attendance => attendance.User).WithMany().HasForeignKey(attendance => attendance.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attendance => new { attendance.UserId, attendance.CheckedInAt });
        builder.HasIndex(attendance => attendance.CheckedInAt);
    }
}
