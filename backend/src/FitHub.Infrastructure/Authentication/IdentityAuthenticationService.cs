using FitHub.Application.Authentication;
using FitHub.Domain.Auditing;
using FitHub.Domain.Identity;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Authentication;

internal sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    FitHubDbContext dbContext,
    JwtTokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthenticationResult> RegisterAsync(
        RegisterCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(command.Email) is not null)
        {
            await AuditAsync("RegistrationRejected", null, ipAddress, "Registration could not be completed.", cancellationToken);
            return AuthenticationResult.Failed();
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = command.Email.Trim(),
            Email = command.Email.Trim(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            IsActive = true
        };
        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            await AuditAsync("RegistrationRejected", null, ipAddress, "Registration could not be completed.", cancellationToken);
            return AuthenticationResult.Failed();
        }

        var roleResult = await userManager.AddToRoleAsync(user, AuthorizationPolicies.Member);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            await AuditAsync("RegistrationRejected", null, ipAddress, "Registration could not be completed.", cancellationToken);
            return AuthenticationResult.Failed();
        }

        var roles = await userManager.GetRolesAsync(user);
        var tokens = tokenIssuer.CreateTokens(user, roles.ToArray());
        dbContext.RefreshTokens.Add(CreateRefreshToken(user.Id, tokens, ipAddress));
        await AuditAsync("RegistrationSucceeded", user.Id, ipAddress, "Member account registered.", cancellationToken, saveChanges: false);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthenticationResult.Success(tokens);
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user) || !await userManager.CheckPasswordAsync(user, command.Password))
        {
            if (user is not null && !await userManager.IsLockedOutAsync(user))
            {
                await userManager.AccessFailedAsync(user);
            }

            await AuditAsync("LoginFailed", user?.Id, ipAddress, "Invalid login attempt.", cancellationToken);
            return AuthenticationResult.Failed();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        var tokens = tokenIssuer.CreateTokens(user, roles.ToArray());
        dbContext.RefreshTokens.Add(CreateRefreshToken(user.Id, tokens, ipAddress));
        await AuditAsync("LoginSucceeded", user.Id, ipAddress, "User authenticated.", cancellationToken, saveChanges: false);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthenticationResult.Success(tokens);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = JwtTokenIssuer.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= now || !storedToken.User.IsActive)
        {
            await AuditAsync("RefreshTokenRejected", storedToken?.UserId, ipAddress, "Refresh token was invalid or inactive.", cancellationToken);
            return AuthenticationResult.Failed();
        }

        var roles = await userManager.GetRolesAsync(storedToken.User);
        var tokens = tokenIssuer.CreateTokens(storedToken.User, roles.ToArray());
        var replacement = CreateRefreshToken(storedToken.UserId, tokens, ipAddress);
        storedToken.Revoke(now, ipAddress, replacement.TokenHash);
        dbContext.RefreshTokens.Add(replacement);
        await AuditAsync("RefreshTokenRotated", storedToken.UserId, ipAddress, "Refresh token rotated.", cancellationToken, saveChanges: false);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthenticationResult.Success(tokens);
    }

    public async Task LogoutAsync(
        string? refreshToken,
        Guid? userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = JwtTokenIssuer.HashRefreshToken(refreshToken);
            var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.Revoke(timeProvider.GetUtcNow(), ipAddress);
                userId ??= storedToken.UserId;
            }
        }

        await AuditAsync("Logout", userId, ipAddress, "User session ended.", cancellationToken);
    }

    public async Task RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && user.IsActive)
        {
            _ = await userManager.GeneratePasswordResetTokenAsync(user);
        }

        await AuditAsync("PasswordRecoveryRequested", user?.Id, ipAddress, "Password recovery was requested.", cancellationToken);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToArray());
    }

    private RefreshToken CreateRefreshToken(Guid userId, AuthenticationTokens tokens, string? ipAddress) =>
        new(userId, JwtTokenIssuer.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAt, ipAddress);

    private async Task AuditAsync(
        string action,
        Guid? userId,
        string? ipAddress,
        string details,
        CancellationToken cancellationToken,
        bool saveChanges = true)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = "Authentication",
            EntityId = userId,
            Details = details,
            IpAddress = ipAddress,
            OccurredAt = timeProvider.GetUtcNow()
        });

        if (saveChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
