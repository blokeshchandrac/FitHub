using FitHub.Domain.Common;
namespace FitHub.Domain.Parking;
public sealed class ParkingCamera : Entity
{
    private ParkingCamera() { }
    public ParkingCamera(string cameraCode, string credentialHash) { CameraCode=string.IsNullOrWhiteSpace(cameraCode)?throw new ArgumentException("Camera code is required."):cameraCode.Trim();CredentialHash=credentialHash; }
    public string CameraCode { get; private set; } = string.Empty;
    public string CredentialHash { get; private set; } = string.Empty;
    public DateTimeOffset? LastEventAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void SetCredentialHash(string value)=>CredentialHash=string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Credential hash is required."):value;
    public void RecordEvent(DateTimeOffset at){LastEventAt=at;MarkUpdated(at);}
}
