namespace FitHub.Application.Attendance;

public static class AccessValidationCodes
{
    public const string Valid = "VALID";
    public const string Blocked = "BLOCKED";
    public const string Expired = "EXPIRED";
    public const string MembershipInactive = "MEMBERSHIP_INACTIVE";
    public const string UserDisabled = "USER_DISABLED";
    public const string InvalidToken = "INVALID_TOKEN";
}

public sealed record AccessValidationDto(
    string Code,
    string Message,
    string? CredentialType,
    Guid? UserId,
    string? MemberName,
    DateTimeOffset? MembershipExpiresAt);
