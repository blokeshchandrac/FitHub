namespace FitHub.Application.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> RegisterAsync(RegisterCommand command, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> LoginAsync(LoginCommand command, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, Guid? userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
