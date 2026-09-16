namespace FitHub.Application.Parking;

public static class ParkingSimulationConstants
{
    public const string CameraId = "SIM-PARK-CAMERA-01";
}

public sealed record ParkingSimulationObservationCommand(string SpaceCode, string State, decimal Confidence);
public sealed record ParkingSimulationViolationCommand(
    string VehicleSimulationId,
    IReadOnlyCollection<string> AffectedSpaceIds,
    string ViolationType,
    string Reason);
public sealed record ParkingSimulationScanCommand(
    string SimulationSessionId,
    string SourceDeviceId,
    DateTimeOffset ObservedAt,
    IReadOnlyCollection<ParkingSimulationObservationCommand> Observations,
    IReadOnlyCollection<ParkingSimulationViolationCommand> Violations);
public sealed record ParkingAlertDto(
    Guid Id,
    string SimulationSessionId,
    string VehicleSimulationId,
    IReadOnlyCollection<string> AffectedSpaceIds,
    string ViolationType,
    string SourceDeviceId,
    string Reason,
    string Status,
    DateTimeOffset DetectedAt,
    DateTimeOffset LastObservedAt,
    DateTimeOffset? ResolvedAt);
public sealed record ParkingSimulationScanResultDto(
    int UpdatedSpaces,
    int CreatedAlerts,
    int ResolvedAlerts,
    IReadOnlyCollection<ParkingAlertDto> ActiveAlerts,
    DateTimeOffset AcceptedAt);

public interface IParkingSimulationService
{
    Task<ParkingSimulationScanResultDto> RecordScanAsync(ParkingSimulationScanCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParkingAlertDto>> GetAlertsAsync(bool includeResolved, CancellationToken cancellationToken = default);
}
