using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class WorkoutTemplateConfiguration : EntityConfiguration<WorkoutTemplate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkoutTemplate> builder)
    {
        builder.Property(template => template.Name).HasMaxLength(150).IsRequired();
        builder.Property(template => template.Description).HasMaxLength(2000);
        builder.Property(template => template.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasOne(template => template.OwnerUser).WithMany().HasForeignKey(template => template.OwnerUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(template => template.Exercises).WithMany(exercise => exercise.WorkoutTemplates)
            .UsingEntity(join => join.ToTable("WorkoutTemplateExercises"));
        builder.HasIndex(template => new { template.OwnerUserId, template.Name });
        builder.HasIndex(template => template.IsPublic);
        builder.HasIndex(template => new { template.OwnerUserId, template.Kind });
    }
}
