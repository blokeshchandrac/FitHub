namespace FitHub.Application.Parking;
public interface IParkingRealtimeNotifier
{
    Task ParkingChangedAsync(ParkingChangedDto change,CancellationToken cancellationToken=default);
    Task ParkingAlertChangedAsync(ParkingAlertChangedDto change,CancellationToken cancellationToken=default)=>Task.CompletedTask;
}
