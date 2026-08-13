namespace FitHub.Application.Administration;
public interface IAdministratorDashboardService{Task<AdministratorDashboardDto> GetAsync(CancellationToken cancellationToken=default);Task<CsvReport> ExportAsync(string report,CancellationToken cancellationToken=default);}
