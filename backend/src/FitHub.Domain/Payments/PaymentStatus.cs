namespace FitHub.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
    Cancelled
}
