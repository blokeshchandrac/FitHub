using FitHub.Domain.Common;
namespace FitHub.Domain.Equipment;
public sealed class EquipmentDevice : Entity
{
    private EquipmentDevice() { }
    public EquipmentDevice(Guid equipmentId, string deviceCode, string credentialHash) { EquipmentId = equipmentId; DeviceCode = deviceCode; CredentialHash = credentialHash; }
    public Guid EquipmentId { get; private set; }
    public Equipment Equipment { get; private set; } = null!;
    public string DeviceCode { get; private set; } = string.Empty;
    public string CredentialHash { get; private set; } = string.Empty;
    public DateTimeOffset? LastHeartbeatAt { get; private set; }
    public bool IsConnected { get; private set; }
    public void Heartbeat(DateTimeOffset at) { LastHeartbeatAt = at; IsConnected = true; MarkUpdated(at); }
    public void Disconnect(DateTimeOffset at) { IsConnected = false; MarkUpdated(at); }
    public void SetCredentialHash(string credentialHash) => CredentialHash = string.IsNullOrWhiteSpace(credentialHash) ? throw new ArgumentException("Credential hash is required.") : credentialHash;
}
