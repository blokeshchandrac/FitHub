using FitHub.Application.Parking;
using FitHub.Domain.Parking;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.ParkingServices;

public sealed class ParkingSimulationService(
    FitHubDbContext db,
    IParkingRealtimeNotifier notifier,
    TimeProvider timeProvider) : IParkingSimulationService
{
    private static readonly HashSet<string> SimulationSpaceCodes =
        Enumerable.Range(0, 3)
            .SelectMany(row => Enumerable.Range(1, 4).Select(number => $"P-{(char)('A' + row)}{number}"))
            .ToHashSet(StringComparer.Ordinal);

    public async Task<ParkingSimulationScanResultDto> RecordScanAsync(ParkingSimulationScanCommand command, CancellationToken ct = default)
    {
        Validate(command);
        var now = timeProvider.GetUtcNow();
        if (command.ObservedAt > now.AddMinutes(5)) throw new ArgumentException("Parking observation time cannot be in the future.");
        if (command.ObservedAt < now.AddMinutes(-5)) throw new ArgumentException("The parking observation batch is stale.");

        var spaces = await db.ParkingSpaces
            .Where(x => SimulationSpaceCodes.Contains(x.Code) && x.IsActive)
            .ToDictionaryAsync(x => x.Code, StringComparer.Ordinal, ct);
        var missing = SimulationSpaceCodes.Except(spaces.Keys).OrderBy(x => x).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"Simulation parking spaces are missing: {string.Join(", ", missing)}.");

        var camera = await db.ParkingCameras.SingleOrDefaultAsync(x => x.CameraCode == ParkingSimulationConstants.CameraId && x.IsActive, ct)
            ?? throw new InvalidOperationException("The development parking simulation camera is not configured.");
        var activeAlerts = await db.ParkingViolationAlerts
            .Where(x => x.SimulationSessionId == command.SimulationSessionId && x.IsActive)
            .ToListAsync(ct);

        var changedSpaces = new List<ParkingSpace>();
        foreach (var observation in command.Observations)
        {
            var space = spaces[observation.SpaceCode];
            var state = ParseState(observation.State);
            space.RecordDetection(state, observation.Confidence, ParkingSimulationConstants.CameraId, command.ObservedAt);
            changedSpaces.Add(space);
        }

        var submittedKeys = new HashSet<string>(StringComparer.Ordinal);
        var changedAlerts = new List<ParkingViolationAlert>();
        var createdAlerts = 0;
        foreach (var violation in command.Violations)
        {
            var affected = violation.AffectedSpaceIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            // The stable session/vehicle/space key turns repeated scans of one unchanged obstruction
            // into observations of the same alert instead of duplicate Administrator notifications.
            var key = BuildKey(command.SimulationSessionId, violation.VehicleSimulationId, affected);
            submittedKeys.Add(key);
            var alert = await db.ParkingViolationAlerts.SingleOrDefaultAsync(x => x.DeduplicationKey == key, ct);
            if (alert is null)
            {
                alert = new ParkingViolationAlert(command.SimulationSessionId, violation.VehicleSimulationId,
                    string.Join(',', affected), violation.ViolationType, ParkingSimulationConstants.CameraId, violation.Reason, key, command.ObservedAt);
                db.ParkingViolationAlerts.Add(alert);
                createdAlerts++;
            }
            else alert.Observe(command.ObservedAt, violation.Reason);
            changedAlerts.Add(alert);
        }

        var resolved = 0;
        foreach (var alert in activeAlerts.Where(x => !submittedKeys.Contains(x.DeduplicationKey)))
        {
            // Only a later complete scan can resolve an alert, preventing missing partial data from
            // being interpreted as proof that the obstruction disappeared.
            alert.Resolve(command.ObservedAt);
            changedAlerts.Add(alert);
            resolved++;
        }

        camera.RecordEvent(command.ObservedAt);
        await db.SaveChangesAsync(ct);

        foreach (var space in changedSpaces)
            await notifier.ParkingChangedAsync(ToChanged(space), ct);
        foreach (var alert in changedAlerts)
            await notifier.ParkingAlertChangedAsync(ToChanged(alert), ct);

        var currentEntities = await db.ParkingViolationAlerts.AsNoTracking()
            .Where(x => x.SimulationSessionId == command.SimulationSessionId && x.IsActive)
            .OrderByDescending(x => x.DetectedAt)
            .ToListAsync(ct);
        var current = currentEntities.Select(ToDto).ToArray();
        return new ParkingSimulationScanResultDto(changedSpaces.Count, createdAlerts, resolved, current, now);
    }

    public async Task<IReadOnlyCollection<ParkingAlertDto>> GetAlertsAsync(bool includeResolved, CancellationToken ct = default)
    {
        var alerts = await db.ParkingViolationAlerts.AsNoTracking()
            .Where(x => includeResolved || x.IsActive)
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.DetectedAt)
            .Take(100)
            .ToListAsync(ct);
        return alerts.Select(ToDto).ToArray();
    }

    private static void Validate(ParkingSimulationScanCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SimulationSessionId) || command.SimulationSessionId.Length > 64)
            throw new ArgumentException("A valid simulation session identifier is required.");
        if (!string.Equals(command.SourceDeviceId, ParkingSimulationConstants.CameraId, StringComparison.Ordinal))
            throw new ArgumentException("The parking simulation camera identifier is invalid.");
        if (command.Observations.Count != SimulationSpaceCodes.Count ||
            command.Observations.Select(x => x.SpaceCode).Distinct(StringComparer.Ordinal).Count() != SimulationSpaceCodes.Count ||
            command.Observations.Any(x => !SimulationSpaceCodes.Contains(x.SpaceCode)))
            throw new ArgumentException("A scan must contain one observation for each of the twelve simulation spaces.");
        if (command.Observations.Any(x => x.Confidence is < 0 or > 1))
            throw new ArgumentException("Observation confidence must be between zero and one.");
        foreach (var violation in command.Violations)
        {
            if (string.IsNullOrWhiteSpace(violation.VehicleSimulationId) || violation.VehicleSimulationId.Length > 64)
                throw new ArgumentException("A valid simulation vehicle identifier is required.");
            if (violation.AffectedSpaceIds.Count < 2 || violation.AffectedSpaceIds.Any(x => !SimulationSpaceCodes.Contains(x)))
                throw new ArgumentException("A parking violation must identify valid affected simulation spaces.");
            if (!string.Equals(violation.ViolationType, "TWO_SPACE_OBSTRUCTION", StringComparison.Ordinal))
                throw new ArgumentException("The parking violation type is invalid.");
            if (string.IsNullOrWhiteSpace(violation.Reason) || violation.Reason.Length > 500)
                throw new ArgumentException("A concise parking violation reason is required.");
        }
    }

    private static ParkingState ParseState(string value) => value switch
    {
        "AVAILABLE" => ParkingState.Available,
        "OCCUPIED" => ParkingState.Occupied,
        "MISUSED_OR_BLOCKED" => ParkingState.MisusedOrBlocked,
        "UNKNOWN" => ParkingState.Unknown,
        _ => throw new ArgumentException("The parking observation state is invalid.")
    };

    private static string BuildKey(string sessionId, string vehicleId, IReadOnlyCollection<string> affected) =>
        $"{sessionId}:{vehicleId}:{string.Join('|', affected)}";
    private static ParkingChangedDto ToChanged(ParkingSpace x) =>
        new(x.Id, x.Code, x.CurrentState.ToString(), x.CurrentConfidence, x.CurrentSource, x.LastEventAt);
    private static ParkingAlertChangedDto ToChanged(ParkingViolationAlert x) =>
        new(x.Id, x.VehicleSimulationId, x.AffectedSpaceCodes.Split(',', StringSplitOptions.RemoveEmptyEntries),
            x.IsActive ? "ACTIVE" : "RESOLVED", x.Reason, x.ResolvedAt ?? x.LastObservedAt);
    private static ParkingAlertDto ToDto(ParkingViolationAlert x) =>
        new(x.Id, x.SimulationSessionId, x.VehicleSimulationId,
            x.AffectedSpaceCodes.Split(',', StringSplitOptions.RemoveEmptyEntries), x.ViolationType,
            x.SourceDeviceId, x.Reason, x.IsActive ? "ACTIVE" : "RESOLVED", x.DetectedAt, x.LastObservedAt, x.ResolvedAt);
}
