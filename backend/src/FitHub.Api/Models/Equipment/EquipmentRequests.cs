namespace FitHub.Api.Models.Equipment;
public sealed record ReleaseEquipmentRequest(bool ClearanceConfirmed = false);
public sealed record EquipmentOverrideRequest(string State, string Reason);
public sealed record DeviceCardRequest(string CardUid);
public sealed record DeviceActionRequest(string Action, string? CardUid);
public sealed record SensorEventRequest(bool Detected);
