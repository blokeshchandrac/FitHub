namespace FitHub.Application.Progress;

public interface IProgressService
{
    Task<BodyProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BodyMeasurementDto> LogMeasurementAsync(Guid userId, BodyMeasurementCommand command, CancellationToken cancellationToken = default);
}
