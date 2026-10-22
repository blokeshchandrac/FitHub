using FitHub.Domain.Equipment;
namespace FitHub.UnitTests;
public sealed class EquipmentTests
{
    private readonly Guid _owner = Guid.NewGuid();
    [Fact] public void AvailableCanBeClaimed() { var item = New(); item.Claim(_owner, Now); Assert.Equal(EquipmentState.Occupied, item.CurrentState); Assert.Equal(_owner, item.OwnerUserId); }
    [Fact] public void OccupiedCannotBeClaimedBySecondMember() { var item = Claimed(); Assert.Throws<InvalidOperationException>(() => item.Claim(Guid.NewGuid(), Now)); }
    [Fact] public void OwnerCanEnableAndDisableSharing() { var item = Claimed(); item.AllowSharing(_owner, Now); Assert.Equal(EquipmentState.SharingAllowed, item.CurrentState); item.ReturnToOccupied(_owner, Now); Assert.Equal(EquipmentState.Occupied, item.CurrentState); }

    [Fact]
    public void OwnerCannotDisableSharingWhilePartnerIsAssigned()
    {
        var partner = Guid.NewGuid();
        var item = Claimed();
        item.AllowSharing(_owner, Now);
        item.Claim(partner, Now);

        var error = Assert.Throws<InvalidOperationException>(() => item.ReturnToOccupied(_owner, Now));

        Assert.Contains("partner must finish", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EquipmentState.SharingAllowed, item.CurrentState);
        Assert.Equal(partner, item.PartnerUserId);
    }
    [Fact] public void SharingSupportsOnlyOnePartner() { var item = Claimed(); item.AllowSharing(_owner, Now); item.Claim(Guid.NewGuid(), Now); Assert.Throws<InvalidOperationException>(() => item.Claim(Guid.NewGuid(), Now)); }
    [Fact] public void NonOwnerCannotChangeSharing() { var item = Claimed(); Assert.Throws<UnauthorizedAccessException>(() => item.AllowSharing(Guid.NewGuid(), Now)); }
    [Fact] public void OwnerReleaseMakesClearMachineAvailable() { var item = Claimed(); item.Release(_owner, Now); Assert.Equal(EquipmentState.Available, item.CurrentState); }
    [Fact] public void DetectedObjectBlocksUnconfirmedRelease() { var item = Claimed(); item.RecordPresence(true, Now); Assert.Throws<InvalidOperationException>(() => item.Release(_owner, Now)); }
    [Fact] public void InitialClearReadingEstablishesBaselineWithoutStartingWarning() { var item = Claimed(); Assert.Null(item.RecordPresence(false, Now)); Assert.Equal(EquipmentState.Occupied, item.CurrentState); Assert.Null(item.ClearanceGraceStartedAt); Assert.False(item.PresenceDetected); }
    [Fact] public void SensorClearAfterPresenceStartsDistinctWarningButNeverReleasesImmediately() { var item = Claimed(); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(1)); Assert.Equal(EquipmentState.ClearanceWarning, item.CurrentState); Assert.NotNull(item.ClearanceGraceStartedAt); Assert.Equal(_owner, item.OwnerUserId); }
    [Fact] public void ConfirmedClearanceRequiresElapsedGrace() { var item = Claimed(); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(1)); Assert.Throws<InvalidOperationException>(() => item.ConfirmClearance(Now.AddSeconds(30), TimeSpan.FromSeconds(30))); item.ConfirmClearance(Now.AddSeconds(31), TimeSpan.FromSeconds(30)); Assert.Equal(EquipmentState.Available, item.CurrentState); }
    [Fact] public void HeartbeatRestoresStateBeforeOffline() { var item = Claimed(); item.MarkOffline(Now, "timeout"); item.RestoreFromHeartbeat(Now.AddMinutes(1)); Assert.Equal(EquipmentState.Occupied, item.CurrentState); }
    [Fact] public void AdministratorOverrideRequiresReason() { var item = Claimed(); Assert.Throws<ArgumentException>(() => item.AdministratorOverride(EquipmentState.Maintenance, Now, Guid.NewGuid(), "")); }
    [Fact] public void AdministratorCanOverrideToMaintenance() { var item = Claimed(); item.AdministratorOverride(EquipmentState.Maintenance, Now, Guid.NewGuid(), "Safety inspection"); Assert.Equal(EquipmentState.Maintenance, item.CurrentState); Assert.Null(item.OwnerUserId); }
    [Fact] public void AdministratorResetToAvailableClearsSensorSessionState() { var item = Claimed(); item.RecordPresence(true, Now); item.AdministratorOverride(EquipmentState.Available, Now.AddSeconds(1), Guid.NewGuid(), "Simulation reset"); Assert.Null(item.PresenceDetected); Assert.Null(item.PresenceChangedAt); Assert.Null(item.ClearanceGraceStartedAt); }
    [Theory] [InlineData(EquipmentState.Maintenance)] [InlineData(EquipmentState.Offline)] [InlineData(EquipmentState.Occupied)] public void NonAvailableMachineRejectsNewOwnerClaim(EquipmentState state) { var item = New(); if (state == EquipmentState.Occupied) item.Claim(_owner, Now); else item.AdministratorOverride(state, Now, Guid.NewGuid(), "test"); Assert.Throws<InvalidOperationException>(() => item.Claim(Guid.NewGuid(), Now)); }
    [Fact] public void AvailableMachineRejectsSharingAndRelease() { var item = New(); Assert.Throws<UnauthorizedAccessException>(() => item.AllowSharing(_owner, Now)); Assert.Throws<InvalidOperationException>(() => item.Release(_owner, Now)); }
    [Fact] public void NonOwnerCannotRelease() { var item = Claimed(); Assert.Throws<UnauthorizedAccessException>(() => item.Release(Guid.NewGuid(), Now)); }
    [Fact] public void NewPresenceCancelsClearanceGrace() { var item = Claimed(); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(1)); item.RecordPresence(true, Now.AddSeconds(2)); Assert.Null(item.ClearanceGraceStartedAt); Assert.Equal(EquipmentState.Occupied, item.CurrentState); }
    [Fact] public void ReturningMemberRestoresSharingAfterClearanceWarning() { var item = Claimed(); item.AllowSharing(_owner, Now); item.Claim(Guid.NewGuid(), Now); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(1)); Assert.Equal(EquipmentState.ClearanceWarning, item.CurrentState); item.RecordPresence(true, Now.AddSeconds(2)); Assert.Equal(EquipmentState.SharingAllowed, item.CurrentState); }
    [Fact] public void DuplicateClearObservationDoesNotRestartGracePeriod() { var item = Claimed(); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(1)); var started = item.ClearanceGraceStartedAt; var history = item.StatusHistory.Count; Assert.Null(item.RecordPresence(false, Now.AddSeconds(5))); Assert.Equal(started, item.ClearanceGraceStartedAt); Assert.Equal(history, item.StatusHistory.Count); Assert.Equal(Now.AddSeconds(5), item.PresenceChangedAt); }
    [Fact] public void StaleReturnObservationCannotCancelClearance() { var item = Claimed(); item.RecordPresence(true, Now); item.RecordPresence(false, Now.AddSeconds(5)); Assert.Null(item.RecordPresence(true, Now.AddSeconds(4))); Assert.Equal(EquipmentState.ClearanceWarning, item.CurrentState); Assert.Equal(Now.AddSeconds(5), item.ClearanceGraceStartedAt); Assert.False(item.PresenceDetected); }
    [Fact] public void ClearanceCannotBeConfirmedWithoutClearReading() { var item = Claimed(); Assert.Throws<InvalidOperationException>(() => item.ConfirmClearance(Now.AddMinutes(1), TimeSpan.FromSeconds(30))); }
    private Equipment New() => new("TEST-01", "Test machine");
    private Equipment Claimed() { var item = New(); item.Claim(_owner, Now); return item; }
    private static DateTimeOffset Now => new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
}
