using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ExerciseLogConfiguration : EntityConfiguration<ExerciseLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ExerciseLog> builder)
    {
        builder.Property(log => log.Notes).HasMaxLength(1000);
        builder.HasOne(log => log.WorkoutSession).WithMany(session => session.ExerciseLogs).HasForeignKey(log => log.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(log => log.Exercise).WithMany().HasForeignKey(log => log.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(log => new { log.WorkoutSessionId, log.OrderIndex }).IsUnique();
        builder.HasIndex(log => log.ExerciseId);
    }
}
