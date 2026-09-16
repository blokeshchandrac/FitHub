namespace FitHub.Application.Equipment;
public interface IEquipmentService
{
    Task<IReadOnlyCollection<EquipmentSummaryDto>> ListAsync(bool includeLiveDetails, bool includeMemberIdentities = false, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> GetAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> ClaimAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> AllowSharingAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> ReturnOccupiedAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> ReleaseAsync(Guid equipmentId, Guid userId, bool clearanceConfirmed, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> OverrideAsync(Guid equipmentId, Guid administratorId, AdministratorOverrideCommand command, CancellationToken cancellationToken = default);
    Task DeviceClaimAsync(Guid equipmentId, string cardUid, CancellationToken cancellationToken = default);
    Task DeviceActionAsync(Guid equipmentId, string action, string? cardUid, CancellationToken cancellationToken = default);
    Task SensorAsync(Guid equipmentId, bool detected, CancellationToken cancellationToken = default);
    Task<EquipmentDetailDto?> ConfirmClearanceAsync(Guid equipmentId, TimeSpan gracePeriod, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<int> MarkStaleDevicesOfflineAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
