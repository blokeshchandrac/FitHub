using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class SetLogConfiguration : EntityConfiguration<SetLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SetLog> builder)
    {
        builder.Property(log => log.WeightKg).HasPrecision(8, 2);
        builder.ToTable("SetLogs", table => table.HasCheckConstraint(
            "CK_SetLogs_RestSeconds", "\"RestSeconds\" IS NULL OR (\"RestSeconds\" >= 0 AND \"RestSeconds\" <= 3600)"));
        builder.HasOne(log => log.ExerciseLog).WithMany(exerciseLog => exerciseLog.Sets).HasForeignKey(log => log.ExerciseLogId);
        builder.HasIndex(log => new { log.ExerciseLogId, log.SetNumber }).IsUnique();
    }
}
