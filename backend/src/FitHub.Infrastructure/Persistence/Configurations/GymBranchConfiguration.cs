using FitHub.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class GymBranchConfiguration : EntityConfiguration<GymBranch>
{
    protected override void ConfigureEntity(EntityTypeBuilder<GymBranch> builder)
    {
        builder.Property(branch => branch.Code).HasMaxLength(20).IsRequired();
        builder.Property(branch => branch.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(branch => branch.Code).IsUnique();
        builder.HasIndex(branch => branch.IsActive);
    }
}
