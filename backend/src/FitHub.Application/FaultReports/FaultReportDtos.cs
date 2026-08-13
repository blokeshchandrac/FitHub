namespace FitHub.Application.FaultReports;
public sealed record FaultPhotoUpload(Stream Content, string FileName, string ContentType, long Length);
public sealed record CreateFaultReportCommand(Guid EquipmentId, string Category, string? Comment, bool SeriousSafetyConcern, FaultPhotoUpload? Photo);
public sealed record UpdateFaultReportCommand(string Status, string MaintenanceNotes);
public sealed record FaultReportDto(Guid Id, Guid EquipmentId, string EquipmentName, Guid ReportedByUserId, string Category, string? Comment, bool SeriousSafetyConcern, string Status, string? MaintenanceNotes, DateTimeOffset? ResolutionDate, IReadOnlyCollection<string> ImageUrls, DateTimeOffset CreatedAt);
