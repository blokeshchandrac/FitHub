namespace FitHub.Application.Parking;
public sealed record ParkingSpaceDto(Guid Id,string Code,string Zone,bool IsAccessible,string State,decimal Confidence,string Source,DateTimeOffset EventTime);
public sealed record ParkingOverviewDto(int Total,int Available,int Occupied,int Unknown,int Disabled,IReadOnlyCollection<ParkingSpaceDto> Spaces);
public sealed record ParkingDetectionCommand(string SpaceCode,string State,decimal Confidence,string Source,DateTimeOffset EventTime);
public sealed record ParkingCorrectionCommand(string State,string Reason);
public sealed record CreateParkingSpaceCommand(string Code,string Zone,bool IsAccessible);
public sealed record ParkingChangedDto(Guid SpaceId,string Code,string State,decimal Confidence,string Source,DateTimeOffset EventTime);
