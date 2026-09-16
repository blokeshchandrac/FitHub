using FitHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();
        builder.Property(user=>user.FitnessGoal).HasConversion<string>().HasMaxLength(30);
        builder.Property(user=>user.ActivityLevel).HasConversion<string>().HasMaxLength(30);
        builder.Property(user => user.ExperienceLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(user => user.PlannedTrainingDays).HasConversion<int>();
        builder.Property(user => user.PreferredName).HasMaxLength(50);
        builder.Property(user => user.HeightCm).HasPrecision(5, 1);
        builder.Property(user => user.WeightKg).HasPrecision(5, 1);
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.OnboardingCompleted);
        builder.HasIndex(user => user.PreferredGymBranchId);
        builder.HasOne(user => user.PreferredGymBranch).WithMany().HasForeignKey(user => user.PreferredGymBranchId).OnDelete(DeleteBehavior.SetNull);
    }
}
