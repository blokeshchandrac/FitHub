using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class FaultReportImageConfiguration : EntityConfiguration<FaultReportImage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FaultReportImage> builder)
    {
        builder.Property(image => image.ImageUrl).HasMaxLength(2048).IsRequired();
        builder.Property(image => image.Caption).HasMaxLength(300);
        builder.HasOne(image => image.FaultReport).WithMany(report => report.Images).HasForeignKey(image => image.FaultReportId);
        builder.HasIndex(image => image.FaultReportId);
    }
}
