using FitHub.Domain.Communications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : EntityConfiguration<NotificationPreference>
{
    protected override void ConfigureEntity(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.Property(item => item.Category).HasConversion<string>().HasMaxLength(80).IsRequired();
        builder.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.UserId, item.Category }).IsUnique();
    }
}
