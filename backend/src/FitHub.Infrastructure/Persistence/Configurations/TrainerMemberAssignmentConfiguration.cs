using FitHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class TrainerMemberAssignmentConfiguration : EntityConfiguration<TrainerMemberAssignment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TrainerMemberAssignment> builder)
    {
        builder.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasOne(assignment => assignment.Trainer).WithMany().HasForeignKey(assignment => assignment.TrainerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.Member).WithMany().HasForeignKey(assignment => assignment.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.AssignedByAdministrator).WithMany().HasForeignKey(assignment => assignment.AssignedByAdministratorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.UnassignedByAdministrator).WithMany().HasForeignKey(assignment => assignment.UnassignedByAdministratorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(assignment => assignment.TrainerId);
        builder.HasIndex(assignment => assignment.MemberId);
        builder.HasIndex(assignment => new { assignment.TrainerId, assignment.MemberId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
    }
}
