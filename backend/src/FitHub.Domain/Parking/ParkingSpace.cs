using FitHub.Domain.Common;
namespace FitHub.Domain.Parking;

public sealed class ParkingSpace : Entity
{
    private ParkingSpace() { }
    public ParkingSpace(string code, string zone, ParkingState initialState = ParkingState.Available)
    {
        Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("Parking-space code is required.", nameof(code)) : code.Trim();
        Zone = string.IsNullOrWhiteSpace(zone) ? throw new ArgumentException("Parking zone is required.", nameof(zone)) : zone.Trim();
        CurrentState = initialState; CurrentConfidence = 1m; CurrentSource = "Seed"; LastEventAt = DateTimeOffset.UtcNow;
    }
    public string Code { get; private set; } = string.Empty;
    public string Zone { get; private set; } = string.Empty;
    public bool IsAccessible { get; set; }
    public bool IsActive { get; set; } = true;
    public ParkingState CurrentState { get; private set; }
    public decimal CurrentConfidence { get; private set; }
    public string CurrentSource { get; private set; } = string.Empty;
    public DateTimeOffset LastEventAt { get; private set; }
    public ICollection<ParkingStatusEvent> StatusHistory { get; } = [];

    public ParkingStatusEvent RecordDetection(ParkingState state, decimal confidence, string source, DateTimeOffset occurredAt)
    {
        if (state == ParkingState.Disabled) throw new InvalidOperationException("A detection source cannot disable a parking space.");
        if (CurrentState == ParkingState.Disabled) throw new InvalidOperationException("A disabled parking space ignores detection events until an administrator enables it.");
        return Apply(state, confidence, source, ParkingEventType.Detection, occurredAt, null, null);
    }

    public ParkingStatusEvent AdministratorCorrect(ParkingState state, string reason, DateTimeOffset occurredAt, Guid administratorId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("An administrator correction reason is required.", nameof(reason));
        return Apply(state, 1m, "Administrator", ParkingEventType.AdministratorCorrection, occurredAt, administratorId, reason.Trim());
    }

    private ParkingStatusEvent Apply(ParkingState state, decimal confidence, string source, ParkingEventType eventType, DateTimeOffset occurredAt, Guid? userId, string? reason)
    {
        if (confidence is < 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("A detection source is required.", nameof(source));
        if (occurredAt < LastEventAt) throw new InvalidOperationException("Out-of-order parking events are not accepted.");
        var previous = CurrentState; CurrentState = state; CurrentConfidence = confidence; CurrentSource = source.Trim(); LastEventAt = occurredAt; MarkUpdated(occurredAt);
        var statusEvent = new ParkingStatusEvent(Id, previous, state, confidence, CurrentSource, eventType, occurredAt, userId, reason); StatusHistory.Add(statusEvent); return statusEvent;
    }
}
