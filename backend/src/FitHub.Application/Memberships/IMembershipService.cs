namespace FitHub.Application.Memberships;

public interface IMembershipService
{
    Task<MembershipOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MembershipPlanDto>> GetPlansAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<CheckoutSessionDto> CreateCheckoutAsync(Guid userId, CreateCheckoutCommand command, CancellationToken cancellationToken = default);
    Task<string?> GetReceiptHtmlAsync(Guid userId, Guid paymentId, bool administrator, CancellationToken cancellationToken = default);
    Task<MembershipPlanDto> CreatePlanAsync(UpsertMembershipPlanCommand command, CancellationToken cancellationToken = default);
    Task<MembershipPlanDto?> UpdatePlanAsync(Guid planId, UpsertMembershipPlanCommand command, CancellationToken cancellationToken = default);
    Task<bool> SetPlanActiveAsync(Guid planId, bool active, CancellationToken cancellationToken = default);
    Task<bool> HasActiveMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
}
