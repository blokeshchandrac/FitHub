using FitHub.Api.Infrastructure;
using FitHub.Api.Models.FaultReports;
using FitHub.Application.Authentication;
using FitHub.Application.FaultReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveMembership)]
[Authorize(Policy = AuthorizationPolicies.Member)]
[Route("api/fault-reports")]
public sealed class FaultReportsController(IFaultReportService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<FaultReportDto>> Mine(CancellationToken ct) => service.ListForMemberAsync(User.GetRequiredUserId(), ct);
    [HttpPost]
    [RequestSizeLimit(5_300_000)]
    public async Task<ActionResult<FaultReportDto>> Create([FromForm] CreateFaultReportRequest request, CancellationToken ct)
    {
        FaultPhotoUpload? photo = request.Photo is null ? null : new(request.Photo.OpenReadStream(), request.Photo.FileName, request.Photo.ContentType, request.Photo.Length);
        var result = await service.CreateAsync(User.GetRequiredUserId(), new(request.EquipmentId, request.Category, request.Comment, request.SeriousSafetyConcern, photo), ct);
        return CreatedAtAction(nameof(Mine), result);
    }
}
