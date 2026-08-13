using FitHub.Api.Infrastructure;
using FitHub.Api.Models.FaultReports;
using FitHub.Application.Authentication;
using FitHub.Application.FaultReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Administrator)]
[Route("api/administration/fault-reports")]
public sealed class FaultReportsAdministrationController(IFaultReportService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyCollection<FaultReportDto>> List(CancellationToken ct) => service.ListForAdministrationAsync(ct);
    [HttpPut("{reportId:guid}")]
    public async Task<ActionResult<FaultReportDto>> Update(Guid reportId, UpdateFaultReportRequest request, CancellationToken ct)
    { var result = await service.UpdateAsync(reportId, User.GetRequiredUserId(), new(request.Status, request.MaintenanceNotes), ct); return result is null ? NotFound() : Ok(result); }
}
