namespace FitHub.Application.Parking;
public interface IParkingService
{
    Task<ParkingOverviewDto> GetOverviewAsync(CancellationToken cancellationToken=default);
    Task<ParkingOverviewDto> GetCameraOverviewAsync(CancellationToken cancellationToken=default);
    Task<ParkingSpaceDto> RecordDetectionAsync(string cameraCode,ParkingDetectionCommand command,CancellationToken cancellationToken=default);
    Task<ParkingSpaceDto?> CorrectAsync(Guid spaceId,Guid administratorId,ParkingCorrectionCommand command,CancellationToken cancellationToken=default);
    Task<ParkingSpaceDto> CreateAsync(CreateParkingSpaceCommand command,CancellationToken cancellationToken=default);
}
