using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Parking;

public sealed record ParkingSimulationObservationRequest(
    [Required, StringLength(16)] string SpaceId,
    [Required, StringLength(32)] string Status,
    [Range(0, 1)] decimal Confidence);

public sealed record ParkingSimulationViolationRequest(
    [Required, StringLength(64)] string VehicleSimulationId,
    [Required, MinLength(2), MaxLength(4)] IReadOnlyCollection<string> AffectedSpaceIds,
    [Required, StringLength(64)] string ViolationType,
    [Required, StringLength(500)] string Reason);

public sealed record ParkingSimulationScanRequest(
    [Required, StringLength(64)] string SimulationSessionId,
    [Required, StringLength(80)] string SourceDeviceId,
    DateTimeOffset ObservedAt,
    [Required, MinLength(12), MaxLength(12)] IReadOnlyCollection<ParkingSimulationObservationRequest> Observations,
    [Required] IReadOnlyCollection<ParkingSimulationViolationRequest> Violations);
