using FitHub.Api.Infrastructure;
using FitHub.Api.Simulation;
using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[EnableRateLimiting("simulation")]
[Route("api/simulation")]
public sealed class SimulationController(
    IOptions<SimulationOptions> options,
    SimulationSessionService sessions,
    SimulationMemberCatalogService memberCatalog,
    ConnectedEquipmentSimulationService connectedEquipment,
    IHostEnvironment hostEnvironment,
    ILogger<SimulationController> logger) : ControllerBase
{
    private readonly SimulationOptions settings = options.Value;
    private bool IsAvailable => settings.Enabled &&
        (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"));

    [HttpGet("status")]
    [ProducesResponseType<SimulationStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SimulationStatusResponse>> Status(CancellationToken ct)
    {
        if (!IsAvailable) return NotFound();
        var equipment = await connectedEquipment.GetMappingsAsync(ct);
        return Ok(new SimulationStatusResponse(true, "/hubs/equipment", settings.ClearanceThresholdMetres,
            settings.ClearanceSeconds, equipment));
    }

    [HttpGet("equipment")]
    public async Task<ActionResult<IReadOnlyCollection<SimulationEquipmentMappingResponse>>> Equipment(CancellationToken ct)
    {
        if (!IsAvailable) return NotFound();
        return Ok(await connectedEquipment.GetMappingsAsync(ct));
    }

    [HttpGet("members")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyCollection<SimulationMemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<SimulationMemberResponse>>> Members(CancellationToken cancellationToken)
    {
        if (!IsAvailable) return NotFound();
        return Ok(await memberCatalog.GetMembersAsync(cancellationToken));
    }

    [HttpPost("events")]
    [ProducesResponseType<SimulationEventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SimulationEventResponse>> Process(SimulationEventRequest request, CancellationToken ct)
    {
        if (!IsAvailable) return NotFound();

        var actualMember = request.CardId is null
            ? null
            : await memberCatalog.FindByCardIdAsync(request.CardId, ct);
        if (request.MemberId.HasValue && actualMember?.MemberId != request.MemberId)
            actualMember = null;
        var member = SimulationSessionService.ApplyOverride(actualMember, request.StatusOverride);

        if (string.Equals(request.EventType, "ENTRANCE_SCAN", StringComparison.OrdinalIgnoreCase))
        {
            var denial = SimulationSessionService.ValidateMember(member);
            if (denial is not null)
                return Ok(EntranceResponse(request, false, denial.Value.Code, denial.Value.Message));
            if (!sessions.TryEnter(request.SimulationSessionId, member!.MemberId))
                return Ok(EntranceResponse(request, false, "SESSION_LIMIT_REACHED", "The simulation session is invalid or the session limit was reached."));
            return Ok(EntranceResponse(request, true, "ENTRY_SCAN_APPROVED", $"Access approved — {member.DisplayName} entered the gym."));
        }

        if (string.Equals(request.EventType, "RESET", StringComparison.OrdinalIgnoreCase))
        {
            sessions.Reset(request.SimulationSessionId);
            await connectedEquipment.ResetAsync(User.GetRequiredUserId(), ct);
            return Ok(EntranceResponse(request, true, "RESET", "The connected simulation and persisted equipment mappings were reset."));
        }

        var entered = member is not null && sessions.HasEntered(request.SimulationSessionId, member.MemberId);
        var response = await connectedEquipment.ProcessAsync(request, member, entered, User.GetRequiredUserId(),
            TimeSpan.FromSeconds(settings.ClearanceSeconds), ct);
        logger.LogInformation(
            "Connected simulation event {EventType} for session {SimulationSessionId}, equipment {EquipmentId}: {DecisionCode}",
            request.EventType, request.SimulationSessionId, request.EquipmentId, response.Code);
        return Ok(response);
    }

    private SimulationEventResponse EntranceResponse(SimulationEventRequest request, bool allowed, string code, string message) =>
        new(allowed, code, message, request.SimulationSessionId, request.EquipmentId, "AVAILABLE", [], null,
            DateTimeOffset.UtcNow);
}
