using System.Security.Claims;

namespace FitHub.Api.Infrastructure;

internal static class UserClaimsExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
}
