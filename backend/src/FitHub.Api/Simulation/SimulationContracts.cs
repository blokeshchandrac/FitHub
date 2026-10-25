using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Simulation;

public sealed record SimulationEventRequest(
    [Required, StringLength(64)] string SimulationSessionId,
    [Required, StringLength(64)] string DeviceId,
    [Required, StringLength(64)] string EquipmentId,
    [Required, StringLength(48)] string EventType,
    [StringLength(64)] string? CardId,
    decimal? DistanceMetres,
    DateTimeOffset ObservedAt,
    bool Simulated,
    SimulationStatusOverride StatusOverride = SimulationStatusOverride.Actual,
    Guid? MemberId = null);

public sealed record SimulationEventResponse(
    bool Allowed,
    string Code,
    string Message,
    string SimulationSessionId,
    string EquipmentId,
    string EquipmentState,
    IReadOnlyCollection<string> MemberNames,
    int? ClearanceSecondsRemaining,
    DateTimeOffset OccurredAt,
    Guid? PersistedEquipmentId = null,
    Guid? OwnerMemberId = null,
    Guid? PartnerMemberId = null,
    bool? PresenceDetected = null,
    DateTimeOffset? PresenceChangedAt = null);

public sealed record SimulationStatusResponse(
    bool Enabled,
    string HubUrl,
    decimal ClearanceThresholdMetres,
    int ClearanceSeconds,
    IReadOnlyCollection<SimulationEquipmentMappingResponse> Equipment);

public sealed record SimulationEquipmentMappingResponse(
    string SimulationEquipmentId,
    string DeviceId,
    Guid PersistedEquipmentId,
    string PersistedCode,
    string State,
    Guid? OwnerMemberId,
    Guid? PartnerMemberId,
    bool? PresenceDetected,
    DateTimeOffset? PresenceChangedAt,
    DateTimeOffset? ClearanceStartedAt,
    DateTimeOffset LastUpdatedAt);

public sealed record SimulationMemberResponse(
    Guid MemberId,
    string DisplayName,
    string MembershipStatus,
    string CardId,
    string CardStatus,
    string AccountStatus,
    string CardSource);

public enum SimulationStatusOverride
{
    Actual,
    ActiveMembership,
    ExpiredMembership,
    BlockedCard,
    SuspendedAccount
}

public sealed record SimulationMemberSnapshot(
    Guid MemberId,
    string DisplayName,
    string CardId,
    string MembershipStatus,
    string CardStatus,
    string AccountStatus);
