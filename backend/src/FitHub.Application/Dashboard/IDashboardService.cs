namespace FitHub.Application.Dashboard;

public interface IDashboardService
{
    Task<MemberDashboardDto> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
}
