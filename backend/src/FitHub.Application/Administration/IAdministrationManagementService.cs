namespace FitHub.Application.Administration;

public interface IAdministrationManagementService
{
    Task<IReadOnlyCollection<AdminUserSummaryDto>> ListUsersAsync(string? search, string? role, bool? active, CancellationToken cancellationToken = default);
    Task<AdminUserDetailsDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SetUserActiveAsync(Guid administratorId, Guid userId, bool active, CancellationToken cancellationToken = default);
    Task<bool> SetMembershipEnabledAsync(Guid administratorId, Guid membershipId, bool enabled, CancellationToken cancellationToken = default);
    Task<bool> SetCardActiveAsync(Guid administratorId, Guid cardId, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminUserSummaryDto>> ListTrainersAsync(CancellationToken cancellationToken = default);
    Task<AdminUserSummaryDto> CreateTrainerAsync(Guid administratorId, CreateTrainerCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SystemActivityDto>> ListActivityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminPaymentDto>> ListPaymentsAsync(CancellationToken cancellationToken = default);
}

public sealed record CreateTrainerCommand(string Email,string FirstName,string LastName,string TemporaryPassword);

public sealed record AdminUserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    string MembershipStatus,
    Guid? MembershipId,
    int ActiveCardCount,
    DateTimeOffset CreatedAt);

public sealed record AdminAccessCardDto(Guid Id, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record AdminUserDetailsDto(
    AdminUserSummaryDto User,
    string? PreferredName,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    decimal? WeightKg,
    string FitnessGoal,
    string ActivityLevel,
    string? ExperienceLevel,
    IReadOnlyCollection<string> PlannedTrainingDays,
    Guid? PreferredGymBranchId,
    string? PreferredGymBranchName,
    bool OnboardingCompleted,
    IReadOnlyCollection<AdminAccessCardDto> Cards);

public sealed record SystemActivityDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Details,
    DateTimeOffset OccurredAt);

public sealed record AdminPaymentDto(Guid Id,string MemberEmail,string PlanName,decimal Amount,decimal RefundedAmount,decimal NetAmount,string Currency,string Status,DateTimeOffset CreatedAt,DateTimeOffset? PaidAt,string? CheckoutSessionId,string? PaymentIntentId,bool IsSeededDemo);
