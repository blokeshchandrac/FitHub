using System.Security.Claims;
using FitHub.Application.Authentication;
using FitHub.Application.Memberships;
using Microsoft.AspNetCore.Authorization;

namespace FitHub.Infrastructure.Authorization;

internal sealed class ActiveMembershipHandler(IMembershipService membershipService)
    : AuthorizationHandler<ActiveMembershipRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveMembershipRequirement requirement)
    {
        if (context.User.IsInRole(AuthorizationPolicies.Trainer) || context.User.IsInRole(AuthorizationPolicies.Administrator))
        {
            context.Succeed(requirement);
            return;
        }

        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            && await membershipService.HasActiveMembershipAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
