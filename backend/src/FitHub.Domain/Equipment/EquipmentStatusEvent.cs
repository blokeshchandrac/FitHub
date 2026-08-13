using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Equipment;

public sealed class EquipmentStatusEvent : Entity
{
    private EquipmentStatusEvent() { }
    internal EquipmentStatusEvent(Guid equipmentId, EquipmentState previousState, EquipmentState state, EquipmentEventType eventType, DateTimeOffset occurredAt, Guid? changedByUserId, string? note)
    {
        EquipmentId = equipmentId;
        PreviousState = previousState;
        State = state;
        EventType = eventType;
        OccurredAt = occurredAt;
        ChangedByUserId = changedByUserId;
        Note = note;
    }
    public Guid EquipmentId { get; private set; }
    public Equipment Equipment { get; private set; } = null!;
    public EquipmentState PreviousState { get; private set; }
    public EquipmentState State { get; private set; }
    public EquipmentEventType EventType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public ApplicationUser? ChangedByUser { get; private set; }
    public string? Note { get; private set; }
}
