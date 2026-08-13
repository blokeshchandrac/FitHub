using System.Security.Claims;
using FitHub.Application.Authentication;
using FitHub.Application.Equipment;
using Microsoft.AspNetCore.Authorization;
namespace FitHub.Infrastructure.Authorization;
public sealed class CheckedInEquipmentHandler(IEquipmentAccessService accessService) : AuthorizationHandler<CheckedInEquipmentRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CheckedInEquipmentRequirement requirement)
    {
        if (context.User.IsInRole(AuthorizationPolicies.Trainer) || context.User.IsInRole(AuthorizationPolicies.Administrator)) { context.Succeed(requirement); return; }
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && await accessService.IsCheckedInAsync(id)) context.Succeed(requirement);
    }
}
