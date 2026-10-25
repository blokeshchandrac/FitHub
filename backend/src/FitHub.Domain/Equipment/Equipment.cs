using FitHub.Domain.Common;

namespace FitHub.Domain.Equipment;

public sealed class Equipment : Entity
{
    private Equipment() { }

    public Equipment(string code, string name, EquipmentState initialState = EquipmentState.Available)
    {
        Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("Equipment code is required.", nameof(code)) : code.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Equipment name is required.", nameof(name)) : name.Trim();
        CurrentState = initialState;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public EquipmentState CurrentState { get; private set; }
    public EquipmentState? StateBeforeOffline { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public Guid? PartnerUserId { get; private set; }
    public bool? PresenceDetected { get; private set; }
    public DateTimeOffset? PresenceChangedAt { get; private set; }
    public DateTimeOffset? ClearanceGraceStartedAt { get; private set; }
    public uint Version { get; private set; }
    public ICollection<EquipmentStatusEvent> StatusHistory { get; } = [];

    public EquipmentStatusEvent Claim(Guid userId, DateTimeOffset occurredAt, string source = "member")
    {
        if (CurrentState == EquipmentState.Available)
        {
            OwnerUserId = userId;
            PartnerUserId = null;
            // A new session must establish its own sensor baseline. Retaining the previous
            // session's clear reading could cause the first new clear observation to be ignored.
            PresenceDetected = null;
            PresenceChangedAt = null;
            return TransitionTo(EquipmentState.Occupied, EquipmentEventType.Claimed, occurredAt, userId, source);
        }

        if (CurrentState == EquipmentState.SharingAllowed && OwnerUserId != userId && PartnerUserId is null)
        {
            PartnerUserId = userId;
            return Record(EquipmentEventType.PartnerJoined, occurredAt, userId, source);
        }

        throw new InvalidOperationException("The equipment cannot be claimed in its current state.");
    }

    public EquipmentStatusEvent AllowSharing(Guid userId, DateTimeOffset occurredAt)
    {
        EnsureOwner(userId);
        EnsureState(EquipmentState.Occupied);
        return TransitionTo(EquipmentState.SharingAllowed, EquipmentEventType.SharingEnabled, occurredAt, userId);
    }

    public EquipmentStatusEvent ReturnToOccupied(Guid userId, DateTimeOffset occurredAt)
    {
        EnsureOwner(userId);
        EnsureState(EquipmentState.SharingAllowed);
        if (PartnerUserId.HasValue)
            throw new InvalidOperationException("The sharing partner must finish before sharing can be disabled.");
        return TransitionTo(EquipmentState.Occupied, EquipmentEventType.SharingDisabled, occurredAt, userId);
    }

    public EquipmentStatusEvent Release(Guid userId, DateTimeOffset occurredAt, bool clearanceConfirmed = false)
    {
        if (CurrentState is not (EquipmentState.Occupied or EquipmentState.SharingAllowed))
            throw new InvalidOperationException("Only occupied equipment can be released.");

        if (PartnerUserId == userId)
        {
            PartnerUserId = null;
            return Record(EquipmentEventType.PartnerReleased, occurredAt, userId);
        }

        EnsureOwner(userId);
        if (PresenceDetected == true && !clearanceConfirmed)
            throw new InvalidOperationException("The sensor still detects an object. Confirm clearance before release.");

        if (PartnerUserId.HasValue)
        {
            OwnerUserId = PartnerUserId;
            PartnerUserId = null;
            return TransitionTo(EquipmentState.Occupied, EquipmentEventType.OwnerReleasedToPartner, occurredAt, userId);
        }

        OwnerUserId = null;
        PartnerUserId = null;
        ClearanceGraceStartedAt = null;
        return TransitionTo(EquipmentState.Available, clearanceConfirmed ? EquipmentEventType.ClearanceConfirmed : EquipmentEventType.Released, occurredAt, userId);
    }

    public EquipmentStatusEvent? RecordPresence(bool detected, DateTimeOffset occurredAt)
    {
        // Device observations can be retried or arrive out of order. An older observation must
        // never cancel or restart a newer clearance countdown.
        if (PresenceChangedAt.HasValue && occurredAt <= PresenceChangedAt.Value)
            return null;

        if (PresenceDetected == detected)
        {
            // Advance the observation watermark without adding history or changing the grace start.
            // This prevents a delayed opposite reading from being accepted after a duplicate retry.
            PresenceChangedAt = occurredAt;
            return null;
        }

        var hadPresenceBaseline = PresenceDetected.HasValue;
        PresenceDetected = detected;
        PresenceChangedAt = occurredAt;
        if (detected)
        {
            ClearanceGraceStartedAt = null;
            if (CurrentState == EquipmentState.ClearanceWarning)
            {
                // Sharing is an active-use state, while Clearance Warning is a temporary safety state.
                // Restore the former only when a partner still belongs to the session.
                var restored = PartnerUserId.HasValue ? EquipmentState.SharingAllowed : EquipmentState.Occupied;
                return TransitionTo(restored, EquipmentEventType.ClearanceWarningCancelled, occurredAt);
            }
            return Record(EquipmentEventType.PresenceDetected, occurredAt);
        }

        if (!hadPresenceBaseline)
        {
            // A claim starts with no sensor baseline. An initial clear reading establishes that
            // baseline, but it is not proof that an assigned Member has left the machine.
            ClearanceGraceStartedAt = null;
            return null;
        }

        if (CurrentState is EquipmentState.Occupied or EquipmentState.SharingAllowed)
        {
            // Absence starts a grace period; it never releases equipment on a single clear reading.
            ClearanceGraceStartedAt = occurredAt;
            return TransitionTo(EquipmentState.ClearanceWarning, EquipmentEventType.ClearanceWarningStarted, occurredAt);
        }

        // A clear reading never makes equipment Available. It only starts the grace period.
        return Record(EquipmentEventType.ClearanceWarningStarted, occurredAt);
    }

    public EquipmentStatusEvent ConfirmClearance(DateTimeOffset occurredAt, TimeSpan gracePeriod, Guid? changedByUserId = null)
    {
        if (PresenceDetected != false || ClearanceGraceStartedAt is null || occurredAt - ClearanceGraceStartedAt < gracePeriod)
            throw new InvalidOperationException("Clearance has not remained confirmed for the required grace period.");
        if (CurrentState != EquipmentState.ClearanceWarning)
            throw new InvalidOperationException("Clearance confirmation applies only to equipment in Clearance Warning.");

        OwnerUserId = null;
        PartnerUserId = null;
        ClearanceGraceStartedAt = null;
        return TransitionTo(EquipmentState.Available, EquipmentEventType.ClearanceConfirmed, occurredAt, changedByUserId);
    }

    public EquipmentStatusEvent EnterMaintenance(DateTimeOffset occurredAt, Guid? changedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A maintenance reason is required.", nameof(reason));
        OwnerUserId = null;
        PartnerUserId = null;
        return CurrentState == EquipmentState.Maintenance
            ? Record(EquipmentEventType.MaintenanceTriggered, occurredAt, changedByUserId, reason)
            : TransitionTo(EquipmentState.Maintenance, EquipmentEventType.MaintenanceTriggered, occurredAt, changedByUserId, reason);
    }

    public EquipmentStatusEvent MarkOffline(DateTimeOffset occurredAt, string reason)
    {
        if (CurrentState == EquipmentState.Offline) return Record(EquipmentEventType.HeartbeatMissed, occurredAt, note: reason);
        StateBeforeOffline = CurrentState == EquipmentState.ClearanceWarning
            ? PartnerUserId.HasValue ? EquipmentState.SharingAllowed : EquipmentState.Occupied
            : CurrentState;
        ClearanceGraceStartedAt = null;
        return TransitionTo(EquipmentState.Offline, EquipmentEventType.DeviceDisconnected, occurredAt, note: reason);
    }

    public EquipmentStatusEvent RestoreFromHeartbeat(DateTimeOffset occurredAt)
    {
        if (CurrentState != EquipmentState.Offline) return Record(EquipmentEventType.HeartbeatReceived, occurredAt);
        var restored = StateBeforeOffline ?? EquipmentState.Maintenance;
        StateBeforeOffline = null;
        return TransitionTo(restored, EquipmentEventType.DeviceReconnected, occurredAt);
    }

    public EquipmentStatusEvent AdministratorOverride(EquipmentState state, DateTimeOffset occurredAt, Guid administratorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("An administrator override reason is required.", nameof(reason));
        if (state is EquipmentState.Available or EquipmentState.Maintenance)
        {
            OwnerUserId = null;
            PartnerUserId = null;
            ClearanceGraceStartedAt = null;
        }
        if (state == EquipmentState.Available)
        {
            PresenceDetected = null;
            PresenceChangedAt = null;
        }
        if (state == EquipmentState.Offline) StateBeforeOffline = CurrentState;
        if (state == CurrentState) return Record(EquipmentEventType.AdministratorOverride, occurredAt, administratorId, reason);
        return TransitionTo(state, EquipmentEventType.AdministratorOverride, occurredAt, administratorId, reason);
    }

    private EquipmentStatusEvent TransitionTo(EquipmentState state, EquipmentEventType eventType, DateTimeOffset occurredAt, Guid? userId = null, string? note = null)
    {
        var previous = CurrentState;
        CurrentState = state;
        MarkUpdated(occurredAt);
        var statusEvent = new EquipmentStatusEvent(Id, previous, state, eventType, occurredAt, userId, note);
        StatusHistory.Add(statusEvent);
        return statusEvent;
    }

    private EquipmentStatusEvent Record(EquipmentEventType eventType, DateTimeOffset occurredAt, Guid? userId = null, string? note = null)
    {
        MarkUpdated(occurredAt);
        var statusEvent = new EquipmentStatusEvent(Id, CurrentState, CurrentState, eventType, occurredAt, userId, note);
        StatusHistory.Add(statusEvent);
        return statusEvent;
    }

    private void EnsureOwner(Guid userId)
    {
        if (OwnerUserId != userId) throw new UnauthorizedAccessException("Only the current equipment owner can perform this action.");
    }

    private void EnsureState(EquipmentState state)
    {
        if (CurrentState != state) throw new InvalidOperationException($"Equipment must be {state} for this action.");
    }
}
