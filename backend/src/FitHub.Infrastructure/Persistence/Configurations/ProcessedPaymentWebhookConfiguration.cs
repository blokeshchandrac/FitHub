using FitHub.Domain.Payments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitHub.Infrastructure.Persistence.Configurations;

public sealed class ProcessedPaymentWebhookConfiguration : EntityConfiguration<ProcessedPaymentWebhook>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ProcessedPaymentWebhook> builder)
    {
        builder.Property(item => item.Provider).HasMaxLength(30).IsRequired();
        builder.Property(item => item.EventId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.EventType).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.Provider, item.EventId }).IsUnique();
        builder.HasIndex(item => item.ProcessedAt);
    }
}
