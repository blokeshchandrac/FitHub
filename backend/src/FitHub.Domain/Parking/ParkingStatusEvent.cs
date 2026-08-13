using FitHub.Domain.Common;
using FitHub.Domain.Identity;
namespace FitHub.Domain.Parking;
public sealed class ParkingStatusEvent : Entity
{
    private ParkingStatusEvent() { }
    internal ParkingStatusEvent(Guid parkingSpaceId, ParkingState previousState, ParkingState state, decimal confidence, string source, ParkingEventType eventType, DateTimeOffset occurredAt, Guid? changedByUserId, string? reason)
    { ParkingSpaceId=parkingSpaceId;PreviousState=previousState;State=state;Confidence=confidence;Source=source;EventType=eventType;OccurredAt=occurredAt;ChangedByUserId=changedByUserId;Reason=reason; }
    public Guid ParkingSpaceId { get; private set; }
    public ParkingSpace ParkingSpace { get; private set; } = null!;
    public ParkingState PreviousState { get; private set; }
    public ParkingState State { get; private set; }
    public decimal Confidence { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public ParkingEventType EventType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public ApplicationUser? ChangedByUser { get; private set; }
    public string? Reason { get; private set; }
}
