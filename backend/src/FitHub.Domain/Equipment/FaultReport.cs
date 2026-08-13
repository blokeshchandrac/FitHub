using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Equipment;

public sealed class FaultReport : Entity
{
    private FaultReport() { }
    public FaultReport(Guid equipmentId, Guid reportedByUserId, FaultCategory category, string? comment, bool seriousSafetyConcern)
    {
        if (category == FaultCategory.Other && string.IsNullOrWhiteSpace(comment))
            throw new ArgumentException("A comment is required when the category is Other.", nameof(comment));
        if (seriousSafetyConcern && category != FaultCategory.SafetyConcern)
            throw new ArgumentException("Only a safety concern can be marked as serious.", nameof(seriousSafetyConcern));
        EquipmentId = equipmentId;
        ReportedByUserId = reportedByUserId;
        Category = category;
        Comment = comment?.Trim();
        Title = category.ToString();
        Description = string.IsNullOrWhiteSpace(comment) ? category.ToString() : comment.Trim();
        IsSeriousSafetyConcern = seriousSafetyConcern;
        Priority = seriousSafetyConcern ? FaultPriority.Critical : FaultPriority.Medium;
    }
    public Guid EquipmentId { get; private set; }
    public Equipment Equipment { get; private set; } = null!;
    public Guid ReportedByUserId { get; private set; }
    public ApplicationUser ReportedByUser { get; private set; } = null!;
    public FaultCategory Category { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Comment { get; private set; }
    public bool IsSeriousSafetyConcern { get; private set; }
    public FaultPriority Priority { get; private set; } = FaultPriority.Medium;
    public FaultReportStatus Status { get; private set; } = FaultReportStatus.Open;
    public string? MaintenanceNotes { get; private set; }
    public DateTimeOffset? ResolutionDate { get; private set; }
    public ICollection<FaultReportImage> Images { get; } = [];
    public ICollection<MaintenanceAction> MaintenanceActions { get; } = [];

    public void UpdateStatus(FaultReportStatus status, string maintenanceNotes, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(maintenanceNotes)) throw new ArgumentException("Maintenance notes are required.", nameof(maintenanceNotes));
        Status = status;
        MaintenanceNotes = maintenanceNotes.Trim();
        ResolutionDate = status is FaultReportStatus.Resolved or FaultReportStatus.Rejected ? at : null;
        MarkUpdated(at);
    }
}
