using FitHub.Domain.Common;

namespace FitHub.Domain.Payments;

public sealed class ProcessedPaymentWebhook : Entity
{
    private ProcessedPaymentWebhook() { }

    public ProcessedPaymentWebhook(string provider, string eventId, string eventType, DateTimeOffset processedAt)
    {
        Provider = provider;
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    public string Provider { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }
}
