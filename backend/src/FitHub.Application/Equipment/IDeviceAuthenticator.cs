namespace FitHub.Application.Equipment;
public interface IDeviceAuthenticator { Task<bool> AuthenticateAsync(Guid equipmentId, string deviceCode, string credential, CancellationToken cancellationToken = default); }
