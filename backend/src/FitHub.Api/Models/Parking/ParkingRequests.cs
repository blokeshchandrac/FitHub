namespace FitHub.Api.Models.Parking;
public sealed record ParkingDetectionRequest(string SpaceCode,string State,decimal Confidence,string Source,DateTimeOffset EventTime);
public sealed record ParkingCorrectionRequest(string State,string Reason);
public sealed record CreateParkingSpaceRequest(string Code,string Zone,bool IsAccessible);
