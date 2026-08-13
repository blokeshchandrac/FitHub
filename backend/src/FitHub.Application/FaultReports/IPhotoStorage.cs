namespace FitHub.Application.FaultReports;
public interface IPhotoStorage { Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default); }
