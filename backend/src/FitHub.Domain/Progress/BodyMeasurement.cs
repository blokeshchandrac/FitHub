using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Progress;

public sealed class BodyMeasurement : Entity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTimeOffset RecordedAt { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public string? Notes { get; set; }
}
