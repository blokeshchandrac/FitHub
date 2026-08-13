namespace FitHub.Application.Progress;

public sealed record BodyMeasurementCommand(decimal? WeightKg, decimal? HeightCm, decimal? BodyFatPercentage, decimal? ChestCm, decimal? WaistCm, decimal? HipCm, string? Notes);
public sealed record BodyMeasurementDto(Guid Id, DateTimeOffset RecordedAt, decimal? WeightKg, decimal? HeightCm, decimal? BodyFatPercentage, decimal? ChestCm, decimal? WaistCm, decimal? HipCm, string? Notes);
public sealed record ProgressPointDto(DateTimeOffset RecordedAt, decimal? WeightKg, decimal? BodyFatPercentage, decimal? WaistCm);
public sealed record BodyProgressDto(decimal? LatestWeightKg, decimal? LatestHeightCm, decimal? Bmi, IReadOnlyCollection<ProgressPointDto> Graph, IReadOnlyCollection<BodyMeasurementDto> History);
