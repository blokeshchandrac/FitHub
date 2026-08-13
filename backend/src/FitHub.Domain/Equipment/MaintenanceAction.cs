using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Equipment;

public sealed class MaintenanceAction : Entity
{
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid? FaultReportId { get; set; }
    public FaultReport? FaultReport { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public ApplicationUser? PerformedByUser { get; set; }
    public MaintenanceActionType ActionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset PerformedAt { get; set; }
    public decimal? Cost { get; set; }
}
