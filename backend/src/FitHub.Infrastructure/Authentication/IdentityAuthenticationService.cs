using System.Data;
using System.Security.Cryptography;
using System.Text;
using FitHub.Application.Authentication;
using FitHub.Domain.Auditing;
using FitHub.Domain.Communications;
using FitHub.Domain.Identity;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitHub.Infrastructure.Authentication;

internal sealed class IdentityAuthenticationService(
    UserManager<ApplicationUser> userManager,
    FitHubDbContext dbContext,
    JwtTokenIssuer tokenIssuer,
    TimeProvider timeProvider,
    IPasswordResetEmailSender passwordResetEmailSender,
    IOptions<PasswordResetOptions> passwordResetOptions,
    ILogger<IdentityAuthenticationService> logger) : IAuthenticationService
{
    private readonly PasswordResetOptions _passwordResetOptions = passwordResetOptions.Value;

    public async Task<AuthenticationResult> RegisterAsync(
        RegisterCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!command.ConsentAccepted)
        {
            await AuditAsync("RegistrationRejected", null, ipAddress, "Registration consent was not accepted.", cancellationToken);
            return AuthenticationResult.Failed();
        }

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
        user.RecordRegistrationConsent(timeProvider.GetUtcNow(), RegistrationConsent.CurrentVersion);
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
            var now = timeProvider.GetUtcNow();
            var outstanding = await dbContext.PasswordResetTokens
                .Where(item => item.UserId == user.Id && item.UsedAtUtc == null && item.InvalidatedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var item in outstanding)
            {
                item.Invalidate(now, "Superseded by a newer request");
            }

            var rawToken = CreateRawResetToken();
            var expiresAtUtc = now.AddMinutes(_passwordResetOptions.ExpiryMinutes);
            dbContext.PasswordResetTokens.Add(new PasswordResetToken(
                user.Id,
                HashResetToken(rawToken),
                expiresAtUtc,
                ipAddress));
            await AuditAsync(
                "PasswordRecoveryRequested",
                user.Id,
                ipAddress,
                "Password recovery was requested.",
                cancellationToken,
                saveChanges: false);
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await passwordResetEmailSender.SendPasswordResetAsync(
                    user.Email!,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    CreateResetUrl(user.Email!, rawToken),
                    TimeSpan.FromMinutes(_passwordResetOptions.ExpiryMinutes),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // The public response remains neutral so SMTP availability cannot reveal account existence.
                logger.LogWarning("Password-reset email delivery failed.");
                await AuditAsync(
                    "PasswordRecoveryDeliveryFailed",
                    user.Id,
                    ipAddress,
                    "Password recovery email delivery failed.",
                    cancellationToken);
            }

            return;
        }

        await AuditAsync("PasswordRecoveryRequested", user?.Id, ipAddress, "Password recovery was requested.", cancellationToken);
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashResetToken(command.Token);
        var now = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // A row lock makes single use reliable even when the same link is submitted concurrently.
        var resetToken = await dbContext.PasswordResetTokens
            .FromSqlInterpolated($"SELECT * FROM \"PasswordResetTokens\" WHERE \"TokenHash\" = {tokenHash} FOR UPDATE")
            .Include(item => item.User)
            .SingleOrDefaultAsync(cancellationToken);
        var normalizedEmail = userManager.NormalizeEmail(command.Email);
        if (resetToken is null ||
            !resetToken.CanBeUsedAt(now) ||
            !resetToken.User.IsActive ||
            !string.Equals(resetToken.User.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
        {
            await AuditAsync(
                "PasswordResetRejected",
                resetToken?.UserId,
                ipAddress,
                "Password-reset link was invalid or expired.",
                cancellationToken,
                saveChanges: false);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ResetPasswordResult.InvalidToken();
        }

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(resetToken.User);
        var identityResult = await userManager.ResetPasswordAsync(resetToken.User, identityToken, command.NewPassword);
        if (!identityResult.Succeeded)
        {
            await AuditAsync(
                "PasswordResetRejected",
                resetToken.UserId,
                ipAddress,
                "The replacement password did not satisfy the account policy.",
                cancellationToken,
                saveChanges: false);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ResetPasswordResult.Rejected(identityResult.Errors.Select(error => error.Description));
        }

        resetToken.MarkUsed(now);
        var refreshTokens = await dbContext.RefreshTokens
            .Where(item => item.UserId == resetToken.UserId && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.Revoke(now, ipAddress);
        }

        await AuditAsync(
            "PasswordResetSucceeded",
            resetToken.UserId,
            ipAddress,
            "Password was reset and existing refresh sessions were revoked.",
            cancellationToken,
            saveChanges: false);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ResetPasswordResult.Success();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToArray(), user.OnboardingCompleted);
    }

    private RefreshToken CreateRefreshToken(Guid userId, AuthenticationTokens tokens, string? ipAddress) =>
        new(userId, JwtTokenIssuer.HashRefreshToken(tokens.RefreshToken), tokens.RefreshTokenExpiresAt, ipAddress);

    private string CreateResetUrl(string email, string rawToken) =>
        $"{_passwordResetOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
        $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";

    private static string CreateRawResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashResetToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

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
