using FitHub.Application.Equipment;
using FitHub.Application.Memberships;
using FitHub.Domain.Equipment;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.EquipmentServices;

public sealed class EquipmentService(FitHubDbContext dbContext, IEquipmentRealtimeNotifier notifier, ICardUidHasher cardHasher, IMembershipService membershipService, IEquipmentAccessService accessService, TimeProvider timeProvider) : IEquipmentService
{
    public async Task<IReadOnlyCollection<EquipmentSummaryDto>> ListAsync(bool includeLiveDetails, bool includeMemberIdentities = false, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Equipment.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var memberIds = includeMemberIdentities
            ? items.SelectMany(item => new[] { item.OwnerUserId, item.PartnerUserId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray()
            : [];
        var names = memberIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users.AsNoTracking().Where(user => memberIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => string.IsNullOrWhiteSpace(user.PreferredName)
                    ? (user.FirstName + " " + user.LastName).Trim()
                    : user.PreferredName, cancellationToken);
        return items.Select(x => new EquipmentSummaryDto(
            x.Id, x.Code, x.Name, x.Location,
            includeLiveDetails ? x.CurrentState.ToString() : null,
            includeLiveDetails ? Color(x.CurrentState) : string.Empty,
            includeLiveDetails,
            x.OwnerUserId.HasValue && names.TryGetValue(x.OwnerUserId.Value, out var owner) ? owner : null,
            x.PartnerUserId.HasValue && names.TryGetValue(x.PartnerUserId.Value, out var partner) ? partner : null,
            x.UpdatedAt)).ToList();
    }

    public async Task<EquipmentDetailDto?> GetAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Equipment.AsNoTracking().Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == equipmentId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public Task<EquipmentDetailDto?> ClaimAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default) => ChangeAsync(equipmentId, x => x.Claim(userId, Now()), cancellationToken);
    public Task<EquipmentDetailDto?> AllowSharingAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default) => ChangeAsync(equipmentId, x => x.AllowSharing(userId, Now()), cancellationToken);
    public Task<EquipmentDetailDto?> ReturnOccupiedAsync(Guid equipmentId, Guid userId, CancellationToken cancellationToken = default) => ChangeAsync(equipmentId, x => x.ReturnToOccupied(userId, Now()), cancellationToken);
    public Task<EquipmentDetailDto?> ReleaseAsync(Guid equipmentId, Guid userId, bool clearanceConfirmed, CancellationToken cancellationToken = default) => ChangeAsync(equipmentId, x => x.Release(userId, Now(), clearanceConfirmed), cancellationToken);

    public async Task<EquipmentDetailDto?> OverrideAsync(Guid equipmentId, Guid administratorId, AdministratorOverrideCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<EquipmentState>(command.State, true, out var state)) throw new ArgumentException("Unknown equipment state.", nameof(command));
        return await ChangeAsync(equipmentId, x => x.AdministratorOverride(state, Now(), administratorId, command.Reason), cancellationToken);
    }

    public async Task DeviceClaimAsync(Guid equipmentId, string cardUid, CancellationToken cancellationToken = default)
    {
        var userId = await FindCardUserAsync(cardUid, cancellationToken);
        _ = await ChangeAsync(equipmentId, x => x.Claim(userId, Now(), "device-card"), cancellationToken);
    }

    public async Task DeviceActionAsync(Guid equipmentId, string action, string? cardUid, CancellationToken cancellationToken = default)
    {
        var userId = string.IsNullOrWhiteSpace(cardUid) ? (Guid?)null : await FindCardUserAsync(cardUid, cancellationToken);
        _ = action.ToLowerInvariant() switch
        {
            "sharing-allowed" when userId.HasValue => await ChangeAsync(equipmentId, x => x.AllowSharing(userId.Value, Now()), cancellationToken),
            "occupied" when userId.HasValue => await ChangeAsync(equipmentId, x => x.ReturnToOccupied(userId.Value, Now()), cancellationToken),
            "release" when userId.HasValue => await ChangeAsync(equipmentId, x => x.Release(userId.Value, Now()), cancellationToken),
            "confirm-clearance" => await ChangeAsync(equipmentId, x => x.ConfirmClearance(Now(), TimeSpan.FromSeconds(30)), cancellationToken),
            "maintenance" => await ChangeAsync(equipmentId, x => x.EnterMaintenance(Now(), null, "Device maintenance control"), cancellationToken),
            _ => throw new ArgumentException("The requested device action is invalid or requires a card UID.", nameof(action))
        };
    }

    public async Task SensorAsync(Guid equipmentId, bool detected, CancellationToken cancellationToken = default) => _ = await ChangeAsync(equipmentId, x => x.RecordPresence(detected, Now()), cancellationToken);

    public Task<EquipmentDetailDto?> ConfirmClearanceAsync(Guid equipmentId, TimeSpan gracePeriod, CancellationToken cancellationToken = default) =>
        ChangeAsync(equipmentId, x => x.ConfirmClearance(Now(), gracePeriod), cancellationToken);

    public async Task HeartbeatAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.EquipmentDevices.SingleOrDefaultAsync(x => x.EquipmentId == equipmentId, cancellationToken) ?? throw new KeyNotFoundException();
        var equipment = await dbContext.Equipment.Include(x => x.StatusHistory).SingleAsync(x => x.Id == equipmentId, cancellationToken);
        var now = Now();
        device.Heartbeat(now);
        equipment.RestoreFromHeartbeat(now);
        await SaveAndNotifyAsync(equipment, cancellationToken);
    }

    public async Task DisconnectAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.EquipmentDevices.SingleOrDefaultAsync(x => x.EquipmentId == equipmentId, cancellationToken) ?? throw new KeyNotFoundException();
        var equipment = await dbContext.Equipment.Include(x => x.StatusHistory).SingleAsync(x => x.Id == equipmentId, cancellationToken);
        var now = Now(); device.Disconnect(now); equipment.MarkOffline(now, "Device explicitly disconnected");
        await SaveAndNotifyAsync(equipment, cancellationToken);
    }

    public async Task<int> MarkStaleDevicesOfflineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var cutoff = Now() - timeout;
        var devices = await dbContext.EquipmentDevices.Include(x => x.Equipment).ThenInclude(x => x.StatusHistory)
            .Where(x => x.IsConnected && (x.LastHeartbeatAt == null || x.LastHeartbeatAt < cutoff)).ToListAsync(cancellationToken);
        foreach (var device in devices) { device.Disconnect(Now()); device.Equipment.MarkOffline(Now(), "Heartbeat timeout"); }
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var device in devices) await NotifyAsync(device.Equipment, cancellationToken);
        return devices.Count;
    }

    private async Task<EquipmentDetailDto?> ChangeAsync(Guid equipmentId, Action<Domain.Equipment.Equipment> change, CancellationToken cancellationToken)
    {
        var equipment = await dbContext.Equipment.Include(x => x.StatusHistory).SingleOrDefaultAsync(x => x.Id == equipmentId, cancellationToken);
        if (equipment is null) return null;
        change(equipment);
        await SaveAndNotifyAsync(equipment, cancellationToken);
        return Map(equipment);
    }

    private async Task SaveAndNotifyAsync(Domain.Equipment.Equipment equipment, CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new EquipmentConcurrencyException(); }
        await NotifyAsync(equipment, cancellationToken);
    }
    private Task NotifyAsync(Domain.Equipment.Equipment equipment, CancellationToken ct) => notifier.EquipmentChangedAsync(new(equipment.Id, equipment.CurrentState.ToString(), equipment.Version, Now()), ct);
    private async Task<Guid> FindCardUserAsync(string uid, CancellationToken ct)
    {
        var hash = cardHasher.Hash(uid);
        var card = await dbContext.MemberAccessCards.AsNoTracking().Include(x => x.User).SingleOrDefaultAsync(x => x.UidHash == hash && x.IsActive, ct);
        if (card is null) throw new UnauthorizedAccessException("The access card is not recognized.");
        if (!card.User.IsActive) throw new UnauthorizedAccessException("The card holder account is disabled.");
        if (!await membershipService.HasActiveMembershipAsync(card.UserId, ct) || !await accessService.IsCheckedInAsync(card.UserId, ct))
            throw new UnauthorizedAccessException("The card holder must have an active membership and be checked in.");
        return card.UserId;
    }
    private DateTimeOffset Now() => timeProvider.GetUtcNow();
    private static string Color(EquipmentState state) => state switch
    {
        EquipmentState.Available => "green",
        EquipmentState.Occupied => "red",
        EquipmentState.SharingAllowed => "yellow",
        EquipmentState.ClearanceWarning => "orange",
        EquipmentState.Maintenance or EquipmentState.Offline => "grey",
        _ => "grey"
    };
    private static EquipmentDetailDto Map(Domain.Equipment.Equipment x) => new(x.Id, x.Code, x.Name, x.Description, x.Location, x.CurrentState.ToString(), Color(x.CurrentState), x.OwnerUserId, x.PartnerUserId, x.PresenceDetected, x.ClearanceGraceStartedAt, x.Version, x.StatusHistory.OrderByDescending(h => h.OccurredAt).Select(h => new EquipmentHistoryDto(h.Id, h.PreviousState.ToString(), h.State.ToString(), h.EventType.ToString(), h.OccurredAt, h.ChangedByUserId, h.Note)).ToList());
}
