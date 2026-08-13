namespace FitHub.Api.Models.FaultReports;
public sealed class CreateFaultReportRequest
{
    public Guid EquipmentId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public bool SeriousSafetyConcern { get; init; }
    public IFormFile? Photo { get; init; }
}
public sealed record UpdateFaultReportRequest(string Status, string MaintenanceNotes);
