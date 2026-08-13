using System.Text;using FitHub.Application.Administration;using FitHub.Application.Authentication;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][Authorize(Policy=AuthorizationPolicies.Administrator)][Route("api/administration/dashboard")]
public sealed class AdministratorDashboardController(IAdministratorDashboardService service):ControllerBase
{
    [HttpGet]public Task<AdministratorDashboardDto> Get(CancellationToken ct)=>service.GetAsync(ct);
    [HttpGet("reports/{report}/csv")]public async Task<IActionResult> Export(string report,CancellationToken ct){var result=await service.ExportAsync(report,ct);return File(Encoding.UTF8.GetBytes(result.Content),"text/csv; charset=utf-8",result.FileName);}
}
