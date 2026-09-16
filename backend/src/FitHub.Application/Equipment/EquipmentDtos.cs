namespace FitHub.Application.Equipment;

public sealed record EquipmentSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Location,
    string? State,
    string StateColor,
    bool LiveDetailsAvailable,
    string? OwnerDisplayName,
    string? PartnerDisplayName,
    DateTimeOffset LastUpdatedAt);
public sealed record EquipmentDetailDto(Guid Id, string Code, string Name, string? Description, string? Location, string State, string StateColor, Guid? OwnerUserId, Guid? PartnerUserId, bool? PresenceDetected, DateTimeOffset? ClearanceGraceStartedAt, uint Version, IReadOnlyCollection<EquipmentHistoryDto> History);
public sealed record EquipmentHistoryDto(Guid Id, string PreviousState, string State, string EventType, DateTimeOffset OccurredAt, Guid? ChangedByUserId, string? Note);
public sealed record EquipmentChangedDto(Guid EquipmentId, string State, uint Version, DateTimeOffset ChangedAt);
public sealed record AdministratorOverrideCommand(string State, string Reason);
public sealed class EquipmentConcurrencyException : Exception { public EquipmentConcurrencyException() : base("The equipment was changed by another request. Refresh and try again.") { } }
