using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class WorkoutSessionConfiguration : EntityConfiguration<WorkoutSession>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(session => session.Notes).HasMaxLength(2000);
        builder.HasOne(session => session.User).WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.WorkoutTemplate).WithMany().HasForeignKey(session => session.WorkoutTemplateId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(session => new { session.UserId, session.StartedAt });
        builder.HasIndex(session => session.WorkoutTemplateId);
    }
}
