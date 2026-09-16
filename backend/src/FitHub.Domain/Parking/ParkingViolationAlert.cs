using FitHub.Domain.Common;

namespace FitHub.Domain.Parking;

public sealed class ParkingViolationAlert : Entity
{
    private ParkingViolationAlert() { }

    public ParkingViolationAlert(
        string simulationSessionId,
        string vehicleSimulationId,
        string affectedSpaceCodes,
        string violationType,
        string sourceDeviceId,
        string reason,
        string deduplicationKey,
        DateTimeOffset detectedAt)
    {
        SimulationSessionId = Required(simulationSessionId, nameof(simulationSessionId));
        VehicleSimulationId = Required(vehicleSimulationId, nameof(vehicleSimulationId));
        AffectedSpaceCodes = Required(affectedSpaceCodes, nameof(affectedSpaceCodes));
        ViolationType = Required(violationType, nameof(violationType));
        SourceDeviceId = Required(sourceDeviceId, nameof(sourceDeviceId));
        Reason = Required(reason, nameof(reason));
        DeduplicationKey = Required(deduplicationKey, nameof(deduplicationKey));
        DetectedAt = detectedAt;
        LastObservedAt = detectedAt;
        IsActive = true;
    }

    public string SimulationSessionId { get; private set; } = string.Empty;
    public string VehicleSimulationId { get; private set; } = string.Empty;
    public string AffectedSpaceCodes { get; private set; } = string.Empty;
    public string ViolationType { get; private set; } = string.Empty;
    public string SourceDeviceId { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string DeduplicationKey { get; private set; } = string.Empty;
    public DateTimeOffset DetectedAt { get; private set; }
    public DateTimeOffset LastObservedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public bool IsActive { get; private set; }

    public void Observe(DateTimeOffset observedAt, string reason)
    {
        if (observedAt < LastObservedAt) throw new InvalidOperationException("Out-of-order parking alerts are not accepted.");
        if (!IsActive)
        {
            DetectedAt = observedAt;
            ResolvedAt = null;
            IsActive = true;
        }
        LastObservedAt = observedAt;
        Reason = Required(reason, nameof(reason));
        MarkUpdated(observedAt);
    }

    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (!IsActive) return;
        if (resolvedAt < LastObservedAt) resolvedAt = LastObservedAt;
        IsActive = false;
        ResolvedAt = resolvedAt;
        MarkUpdated(resolvedAt);
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A value is required.", name) : value.Trim();
}
