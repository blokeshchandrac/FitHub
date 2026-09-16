using FitHub.Application.Equipment;

namespace FitHub.Api.Simulation;

public sealed class ConnectedEquipmentSimulationService(
    IEquipmentService equipmentService,
    SimulationMemberCatalogService memberCatalog,
    TimeProvider timeProvider)
{
    private static readonly SimulationEquipmentDefinition[] Definitions =
    [
        new("EQ-TREADMILL-01", "SIM-ESP32-01", "TREAD-001"),
        new("EQ-BENCH-01", "SIM-ESP32-02", "RACK-001"),
        new("EQ-BIKE-01", "SIM-ESP32-03", "BIKE-001"),
        new("EQ-CABLE-01", "SIM-ESP32-04", "CABLE-001")
    ];

    public async Task<IReadOnlyCollection<SimulationEquipmentMappingResponse>> GetMappingsAsync(CancellationToken ct = default)
    {
        var equipment = await equipmentService.ListAsync(includeLiveDetails: true, cancellationToken: ct);
        var byCode = equipment.ToDictionary(item => item.Code, StringComparer.Ordinal);
        var mappings = new List<SimulationEquipmentMappingResponse>(Definitions.Length);
        foreach (var definition in Definitions)
        {
            if (!byCode.TryGetValue(definition.PersistedCode, out var summary)) continue;
            var detail = await equipmentService.GetAsync(summary.Id, ct);
            if (detail is not null) mappings.Add(Map(definition, detail));
        }
        return mappings;
    }

    public async Task<SimulationEventResponse> ProcessAsync(
        SimulationEventRequest request,
        SimulationMemberSnapshot? member,
        bool memberEntered,
        Guid administratorId,
        TimeSpan clearanceGrace,
        CancellationToken ct = default)
    {
        // Connected Mode deliberately delegates to IEquipmentService: it must exercise the same
        // domain transitions, persistence, audit history, and EquipmentHub notifications as real hardware.
        if (!request.Simulated)
            return await DeniedAsync(request, "SIMULATION_FLAG_REQUIRED", "Only explicitly marked simulation events are accepted.", ct);
        if (!SimulationSessionService.IsValidSession(request.SimulationSessionId))
            return await DeniedAsync(request, "INVALID_SIMULATION_SESSION", "The simulation session identifier is invalid.", ct);
        var definition = Definitions.SingleOrDefault(item => item.SimulationEquipmentId == request.EquipmentId);
        if (definition is null || definition.DeviceId != request.DeviceId)
            return await DeniedAsync(request, "INVALID_SIMULATION_DEVICE", "The simulation equipment or device identifier is invalid.", ct);

        var mapping = (await GetMappingsAsync(ct)).SingleOrDefault(item => item.SimulationEquipmentId == request.EquipmentId);
        if (mapping is null)
            return EmptyResponse(request, false, "EQUIPMENT_MAPPING_MISSING", $"No persisted equipment record is mapped to {request.EquipmentId}.");

        var eventType = request.EventType.Trim().ToUpperInvariant();
        if (RequiresMember(eventType))
        {
            var denial = SimulationSessionService.ValidateMember(member);
            if (denial is not null) return await DeniedAsync(request, denial.Value.Code, denial.Value.Message, ct);
            if (!memberEntered)
                return await DeniedAsync(request, "MEMBER_NOT_CHECKED_IN", "Access denied — the Member is not checked in to this virtual gym session.", ct);
        }

        try
        {
            var before = await equipmentService.GetAsync(mapping.PersistedEquipmentId, ct)
                ?? throw new KeyNotFoundException("The mapped equipment record no longer exists.");
            if (eventType == "NFC_TAPPED" && before.State == "SharingAllowed" && before.PartnerUserId.HasValue)
                return await DeniedAsync(request, "SHARING_FULL", "Access denied — this machine already has its one sharing partner.", ct);
            EquipmentDetailDto? changed = eventType switch
            {
                "NFC_TAPPED" => await equipmentService.ClaimAsync(mapping.PersistedEquipmentId, member!.MemberId, ct),
                "SHARING_ENABLED" => await equipmentService.AllowSharingAsync(mapping.PersistedEquipmentId, member!.MemberId, ct),
                "SHARING_DISABLED" => await equipmentService.ReturnOccupiedAsync(mapping.PersistedEquipmentId, member!.MemberId, ct),
                "SESSION_FINISHED" => await FinishAsync(before, member!.MemberId, ct),
                "SENSOR_ZONE_EXITED" or "CLEARANCE_COUNTDOWN_STARTED" => await StartClearanceAsync(before, ct),
                "SENSOR_ZONE_ENTERED" or "CLEARANCE_COUNTDOWN_CANCELLED" => await CancelClearanceAsync(before, ct),
                "AUTOMATIC_RELEASE_COMPLETED" => await equipmentService.ConfirmClearanceAsync(mapping.PersistedEquipmentId, clearanceGrace, ct),
                "POWER_DISCONNECTED" or "SENSOR_DISCONNECTED" or "SENSOR_FAULT" => await DisconnectAsync(mapping.PersistedEquipmentId, ct),
                "POWER_RECONNECTED" or "SENSOR_RECONNECTED" => await ReconnectAsync(mapping.PersistedEquipmentId, ct),
                "SAFETY_LOCKED" => await equipmentService.OverrideAsync(mapping.PersistedEquipmentId, administratorId, new("Maintenance", "Development equipment simulation safety lock"), ct),
                "SAFETY_UNLOCKED" => await equipmentService.OverrideAsync(mapping.PersistedEquipmentId, administratorId, new("Available", "Development equipment simulation safety lock cleared"), ct),
                "NFC_DISCONNECTED" or "NFC_RECONNECTED" or "LED_DISCONNECTED" or "LED_RECONNECTED" => before,
                _ => null
            };
            if (changed is null)
                return await DeniedAsync(request, "UNKNOWN_EVENT_TYPE", "The connected equipment event type is not supported.", ct);

            var code = eventType == "NFC_TAPPED"
                ? before.State == "SharingAllowed" ? "SECOND_MEMBER_JOINED" : "MACHINE_CLAIMED"
                : eventType;
            return await ResponseAsync(request, true, code, SuccessMessage(code), changed, definition, clearanceGrace, ct);
        }
        catch (UnauthorizedAccessException)
        {
            return await DeniedAsync(request, "NOT_EQUIPMENT_OWNER", "Only the assigned equipment owner may perform this action.", ct);
        }
        catch (InvalidOperationException exception)
        {
            var code = exception.Message.Contains("cannot be claimed", StringComparison.OrdinalIgnoreCase)
                ? "EQUIPMENT_OCCUPIED"
                : "INVALID_TRANSITION";
            return await DeniedAsync(request, code, exception.Message, ct);
        }
        catch (KeyNotFoundException exception)
        {
            return EmptyResponse(request, false, "EQUIPMENT_MAPPING_MISSING", exception.Message);
        }
    }

    public async Task ResetAsync(Guid administratorId, CancellationToken ct = default)
    {
        foreach (var mapping in await GetMappingsAsync(ct))
            await equipmentService.OverrideAsync(mapping.PersistedEquipmentId, administratorId,
                new("Available", "Development equipment simulation reset"), ct);
    }

    private async Task<EquipmentDetailDto?> FinishAsync(EquipmentDetailDto before, Guid memberId, CancellationToken ct)
    {
        if (before.PartnerUserId == memberId || (before.OwnerUserId == memberId && before.PartnerUserId.HasValue))
            return await equipmentService.ReleaseAsync(before.Id, memberId, clearanceConfirmed: false, ct);
        if (before.OwnerUserId != memberId) throw new UnauthorizedAccessException();
        return before;
    }

    private async Task<EquipmentDetailDto?> StartClearanceAsync(EquipmentDetailDto before, CancellationToken ct)
    {
        if (before.State is "Occupied" or "SharingAllowed") await equipmentService.SensorAsync(before.Id, detected: false, ct);
        return await equipmentService.GetAsync(before.Id, ct);
    }

    private async Task<EquipmentDetailDto?> CancelClearanceAsync(EquipmentDetailDto before, CancellationToken ct)
    {
        if (before.State == "ClearanceWarning") await equipmentService.SensorAsync(before.Id, detected: true, ct);
        return await equipmentService.GetAsync(before.Id, ct);
    }

    private async Task<EquipmentDetailDto?> DisconnectAsync(Guid equipmentId, CancellationToken ct)
    {
        await equipmentService.DisconnectAsync(equipmentId, ct);
        return await equipmentService.GetAsync(equipmentId, ct);
    }

    private async Task<EquipmentDetailDto?> ReconnectAsync(Guid equipmentId, CancellationToken ct)
    {
        await equipmentService.HeartbeatAsync(equipmentId, ct);
        return await equipmentService.GetAsync(equipmentId, ct);
    }

    private async Task<SimulationEventResponse> DeniedAsync(SimulationEventRequest request, string code, string message, CancellationToken ct)
    {
        var definition = Definitions.SingleOrDefault(item => item.SimulationEquipmentId == request.EquipmentId);
        if (definition is null) return EmptyResponse(request, false, code, message);
        var mapping = (await GetMappingsAsync(ct)).SingleOrDefault(item => item.SimulationEquipmentId == request.EquipmentId);
        if (mapping is null) return EmptyResponse(request, false, code, message);
        var detail = await equipmentService.GetAsync(mapping.PersistedEquipmentId, ct);
        return detail is null
            ? EmptyResponse(request, false, code, message)
            : await ResponseAsync(request, false, code, message, detail, definition, TimeSpan.FromSeconds(30), ct);
    }

    private async Task<SimulationEventResponse> ResponseAsync(
        SimulationEventRequest request,
        bool allowed,
        string code,
        string message,
        EquipmentDetailDto detail,
        SimulationEquipmentDefinition definition,
        TimeSpan clearanceGrace,
        CancellationToken ct)
    {
        var members = await memberCatalog.GetMembersAsync(ct);
        var namesById = members.ToDictionary(item => item.MemberId, item => item.DisplayName);
        var names = new[] { detail.OwnerUserId, detail.PartnerUserId }
            .Where(id => id.HasValue && namesById.ContainsKey(id.Value))
            .Select(id => namesById[id!.Value])
            .ToArray();
        int? remaining = detail.State == "ClearanceWarning" && detail.ClearanceGraceStartedAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((detail.ClearanceGraceStartedAt.Value.Add(clearanceGrace) - timeProvider.GetUtcNow()).TotalSeconds))
            : null;
        return new SimulationEventResponse(allowed, code, message, request.SimulationSessionId,
            definition.SimulationEquipmentId, ToSimulationState(detail.State), names, remaining,
            timeProvider.GetUtcNow(), detail.Id, detail.OwnerUserId, detail.PartnerUserId);
    }

    private SimulationEquipmentMappingResponse Map(SimulationEquipmentDefinition definition, EquipmentDetailDto detail)
    {
        var updatedAt = detail.History.FirstOrDefault()?.OccurredAt ?? timeProvider.GetUtcNow();
        return new(definition.SimulationEquipmentId, definition.DeviceId, detail.Id, detail.Code,
            ToSimulationState(detail.State), detail.OwnerUserId, detail.PartnerUserId,
            detail.ClearanceGraceStartedAt, updatedAt);
    }

    private static bool RequiresMember(string eventType) => eventType is
        "NFC_TAPPED" or "SHARING_ENABLED" or "SHARING_DISABLED" or "SESSION_FINISHED";

    private static string SuccessMessage(string code) => code switch
    {
        "MACHINE_CLAIMED" => "The persisted equipment session was claimed.",
        "SECOND_MEMBER_JOINED" => "The sharing partner joined the persisted equipment session.",
        "SHARING_ENABLED" => "Sharing is enabled; one additional authorized Member may join.",
        "SHARING_DISABLED" => "Sharing is disabled and the machine is occupied by its owner.",
        "CLEARANCE_COUNTDOWN_STARTED" => "Clearance Warning started in the persisted equipment workflow.",
        "CLEARANCE_COUNTDOWN_CANCELLED" => "Clearance Warning was cancelled because an assigned Member returned.",
        "AUTOMATIC_RELEASE_COMPLETED" => "The persisted equipment session was released after confirmed clearance.",
        _ => "The connected equipment event was accepted and persisted."
    };

    private SimulationEventResponse EmptyResponse(SimulationEventRequest request, bool allowed, string code, string message) =>
        new(allowed, code, message, request.SimulationSessionId, request.EquipmentId, "OFFLINE_UNKNOWN", [], null, timeProvider.GetUtcNow());

    private static string ToSimulationState(string state) => state switch
    {
        "Available" => "AVAILABLE",
        "Occupied" => "OCCUPIED",
        "SharingAllowed" => "SHARING_ALLOWED",
        "ClearanceWarning" => "CLEARANCE_WARNING",
        "Maintenance" => "TEMPORARILY_UNAVAILABLE",
        "Offline" => "OFFLINE_UNKNOWN",
        _ => "OFFLINE_UNKNOWN"
    };

    private sealed record SimulationEquipmentDefinition(string SimulationEquipmentId, string DeviceId, string PersistedCode);
}
