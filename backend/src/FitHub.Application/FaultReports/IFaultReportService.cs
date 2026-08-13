namespace FitHub.Application.FaultReports;
public interface IFaultReportService
{
    Task<FaultReportDto> CreateAsync(Guid userId, CreateFaultReportCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FaultReportDto>> ListForMemberAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FaultReportDto>> ListForAdministrationAsync(CancellationToken cancellationToken = default);
    Task<FaultReportDto?> UpdateAsync(Guid reportId, Guid administratorId, UpdateFaultReportCommand command, CancellationToken cancellationToken = default);
}
