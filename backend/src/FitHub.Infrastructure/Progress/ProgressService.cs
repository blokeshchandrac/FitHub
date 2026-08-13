using FitHub.Application.Analytics;
using FitHub.Application.Progress;
using FitHub.Domain.Progress;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Progress;

internal sealed class ProgressService(FitHubDbContext dbContext, TimeProvider timeProvider) : IProgressService
{
    public async Task<BodyProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var history = await dbContext.BodyMeasurements.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.RecordedAt).Take(100).ToListAsync(cancellationToken);
        var latestWeight = history.FirstOrDefault(item => item.WeightKg is not null)?.WeightKg;
        var latestHeight = history.FirstOrDefault(item => item.HeightCm is not null)?.HeightCm;
        var graph = history.OrderBy(item => item.RecordedAt)
            .Select(item => new ProgressPointDto(item.RecordedAt, item.WeightKg, item.BodyFatPercentage, item.WaistCm)).ToList();
        return new BodyProgressDto(latestWeight, latestHeight, AnalyticsRules.Bmi(latestHeight, latestWeight), graph, history.Select(Map).ToList());
    }

    public async Task<BodyMeasurementDto> LogMeasurementAsync(Guid userId, BodyMeasurementCommand command, CancellationToken cancellationToken = default)
    {
        ProgressValidators.Validate(command);
        var measurement = new BodyMeasurement
        {
            UserId = userId,
            RecordedAt = timeProvider.GetUtcNow(),
            WeightKg = command.WeightKg,
            HeightCm = command.HeightCm,
            BodyFatPercentage = command.BodyFatPercentage,
            ChestCm = command.ChestCm,
            WaistCm = command.WaistCm,
            HipCm = command.HipCm,
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim()
        };
        dbContext.BodyMeasurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(measurement);
    }

    private static BodyMeasurementDto Map(BodyMeasurement item) => new(item.Id, item.RecordedAt, item.WeightKg, item.HeightCm, item.BodyFatPercentage, item.ChestCm, item.WaistCm, item.HipCm, item.Notes);
}
