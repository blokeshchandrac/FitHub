using FitHub.Domain.Communications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class RecommendationConfiguration : EntityConfiguration<Recommendation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Recommendation> builder)
    {
        builder.Property(recommendation => recommendation.Title).HasMaxLength(200).IsRequired();
        builder.Property(recommendation => recommendation.Content).HasMaxLength(4000).IsRequired();
        builder.Property(recommendation => recommendation.Category).HasMaxLength(100).IsRequired();
        builder.Property(recommendation=>recommendation.RuleCode).HasMaxLength(100).IsRequired();
        builder.Property(recommendation=>recommendation.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(recommendation => recommendation.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(recommendation => recommendation.User).WithMany().HasForeignKey(recommendation => recommendation.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(recommendation => new { recommendation.UserId, recommendation.Status });
        builder.HasIndex(recommendation => recommendation.ExpiresAt);
    }
}
