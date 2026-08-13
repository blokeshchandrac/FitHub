using FitHub.Api.Infrastructure;using FitHub.Api.Models.Bookings;using FitHub.Application.Authentication;using FitHub.Application.Bookings;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace FitHub.Api.Controllers;
[ApiController][Authorize(Policy=AuthorizationPolicies.ActiveMembership)][Route("api/classes")]
public sealed class ClassBookingsController(IClassBookingService service):ControllerBase
{
    [HttpGet]public Task<IReadOnlyCollection<ClassScheduleDto>> List(CancellationToken ct)=>service.ListAsync(User.GetRequiredUserId(),ct);
    [HttpPost][Authorize(Policy=AuthorizationPolicies.Staff)]public Task<ClassScheduleDto> Create(CreateClassScheduleRequest request,CancellationToken ct){var creator=User.GetRequiredUserId();var instructor=User.IsInRole(AuthorizationPolicies.Administrator)&&request.InstructorUserId.HasValue?request.InstructorUserId.Value:creator;return service.CreateAsync(new(request.Name,request.Description,request.StartsAt,request.EndsAt,request.Capacity,instructor),ct);}
    [HttpPost("{scheduleId:guid}/book")]public async Task<ActionResult<ClassScheduleDto>> Book(Guid scheduleId,CancellationToken ct){var result=await service.BookAsync(scheduleId,User.GetRequiredUserId(),ct);return result is null?NotFound():Ok(result);}
    [HttpPost("{scheduleId:guid}/cancel")]public async Task<ActionResult<ClassScheduleDto>> Cancel(Guid scheduleId,CancellationToken ct){var result=await service.CancelAsync(scheduleId,User.GetRequiredUserId(),ct);return result is null?NotFound():Ok(result);}
}
