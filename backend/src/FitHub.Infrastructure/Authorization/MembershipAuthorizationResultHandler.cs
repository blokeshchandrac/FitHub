using FitHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Infrastructure.Authorization;

internal sealed class MembershipAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var membershipDenied = authorizeResult.Forbidden
            && policy.Requirements.OfType<ActiveMembershipRequirement>().Any()
            && context.User.IsInRole(AuthorizationPolicies.Member);

        if (!membershipDenied)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Active membership required",
            Detail = "Choose or renew a membership to access this feature."
        };
        problem.Extensions["code"] = "membership_required";
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
    }
}
