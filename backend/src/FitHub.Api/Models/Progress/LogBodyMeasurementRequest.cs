namespace FitHub.Api.Models.Progress;

public sealed record LogBodyMeasurementRequest(decimal? WeightKg, decimal? HeightCm, decimal? BodyFatPercentage, decimal? ChestCm, decimal? WaistCm, decimal? HipCm, string? Notes);
