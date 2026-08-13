using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ExerciseConfiguration : EntityConfiguration<Exercise>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Exercise> builder)
    {
        builder.Property(exercise => exercise.Name).HasMaxLength(150).IsRequired();
        builder.Property(exercise => exercise.Description).HasMaxLength(1000);
        builder.Property(exercise => exercise.MuscleGroup).HasMaxLength(100).IsRequired();
        builder.Property(exercise => exercise.Equipment).HasMaxLength(150);
        builder.Property(exercise => exercise.Instructions).HasMaxLength(4000);
        builder.HasIndex(exercise => exercise.Name).IsUnique();
        builder.HasIndex(exercise => exercise.MuscleGroup);
        builder.HasIndex(exercise => exercise.Equipment);
    }
}
