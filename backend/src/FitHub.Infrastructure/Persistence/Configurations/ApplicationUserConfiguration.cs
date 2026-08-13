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
        builder.HasIndex(user => user.IsActive);
    }
}
