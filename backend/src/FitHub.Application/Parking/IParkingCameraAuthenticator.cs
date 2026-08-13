namespace FitHub.Application.Parking;
public interface IParkingCameraAuthenticator{Task<bool> AuthenticateAsync(string cameraCode,string credential,CancellationToken cancellationToken=default);}
