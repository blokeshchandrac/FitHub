using FitHub.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class WorkoutPlanAssignmentConfiguration : EntityConfiguration<WorkoutPlanAssignment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WorkoutPlanAssignment> builder)
    {
        builder.HasOne(item => item.WorkoutTemplate).WithMany().HasForeignKey(item => item.WorkoutTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.MemberUser).WithMany().HasForeignKey(item => item.MemberUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TrainerUser).WithMany().HasForeignKey(item => item.TrainerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.MemberUserId, item.IsActive });
        builder.HasIndex(item => new { item.WorkoutTemplateId, item.MemberUserId, item.IsActive });
        builder.HasIndex(item => item.TrainerUserId);
    }
}
