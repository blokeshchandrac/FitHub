using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class SetLogConfiguration : EntityConfiguration<SetLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SetLog> builder)
    {
        builder.Property(log => log.WeightKg).HasPrecision(8, 2);
        builder.HasOne(log => log.ExerciseLog).WithMany(exerciseLog => exerciseLog.Sets).HasForeignKey(log => log.ExerciseLogId);
        builder.HasIndex(log => new { log.ExerciseLogId, log.SetNumber }).IsUnique();
    }
}
