using FitHub.Domain.Common;
namespace FitHub.Domain.Equipment;
public sealed class FaultReportImage : Entity
{
    private FaultReportImage() { }
    public FaultReportImage(Guid faultReportId, string imageUrl) { FaultReportId = faultReportId; ImageUrl = imageUrl; }
    public Guid FaultReportId { get; private set; }
    public FaultReport FaultReport { get; private set; } = null!;
    public string ImageUrl { get; private set; } = string.Empty;
    public string? Caption { get; private set; }
}
