using FitHub.Application.Parking;using FitHub.Domain.Parking;using FitHub.Infrastructure.Persistence;using Microsoft.EntityFrameworkCore;
namespace FitHub.Infrastructure.ParkingServices;
public sealed class ParkingService(FitHubDbContext db,IParkingRealtimeNotifier notifier,TimeProvider timeProvider):IParkingService
{
    public async Task<ParkingOverviewDto> GetOverviewAsync(CancellationToken ct=default)=>Overview(await db.ParkingSpaces.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Zone).ThenBy(x=>x.Code).ToListAsync(ct));
    public Task<ParkingOverviewDto> GetCameraOverviewAsync(CancellationToken ct=default)=>GetOverviewAsync(ct);
    public async Task<ParkingSpaceDto> RecordDetectionAsync(string cameraCode,ParkingDetectionCommand command,CancellationToken ct=default)
    {
        if(!Enum.TryParse<ParkingState>(command.State,true,out var state))throw new ArgumentException("Unknown parking state.");
        if(command.Source.Length>100)throw new ArgumentException("Source must not exceed 100 characters.");
        if(command.EventTime>timeProvider.GetUtcNow().AddMinutes(5))throw new ArgumentException("Parking event time cannot be in the future.");
        var space=await db.ParkingSpaces.SingleOrDefaultAsync(x=>x.Code==command.SpaceCode&&x.IsActive,ct)??throw new KeyNotFoundException("Parking space not found.");
        var camera=await db.ParkingCameras.SingleAsync(x=>x.CameraCode==cameraCode,ct);space.RecordDetection(state,command.Confidence,command.Source,command.EventTime);camera.RecordEvent(command.EventTime);await SaveNotify(space,ct);return Map(space);
    }
    public async Task<ParkingSpaceDto?> CorrectAsync(Guid id,Guid adminId,ParkingCorrectionCommand command,CancellationToken ct=default)
    {if(!Enum.TryParse<ParkingState>(command.State,true,out var state))throw new ArgumentException("Unknown parking state.");var space=await db.ParkingSpaces.SingleOrDefaultAsync(x=>x.Id==id,ct);if(space is null)return null;space.AdministratorCorrect(state,command.Reason,timeProvider.GetUtcNow(),adminId);await SaveNotify(space,ct);return Map(space);}
    public async Task<ParkingSpaceDto> CreateAsync(CreateParkingSpaceCommand command,CancellationToken ct=default)
    {if(string.IsNullOrWhiteSpace(command.Code)||command.Code.Length>50||string.IsNullOrWhiteSpace(command.Zone)||command.Zone.Length>100)throw new ArgumentException("A valid code and zone are required.");if(await db.ParkingSpaces.AnyAsync(x=>x.Code==command.Code.Trim(),ct))throw new InvalidOperationException("Parking-space code already exists.");var space=new ParkingSpace(command.Code,command.Zone){IsAccessible=command.IsAccessible};db.ParkingSpaces.Add(space);await db.SaveChangesAsync(ct);await notifier.ParkingChangedAsync(new(space.Id,space.Code,space.CurrentState.ToString(),space.CurrentConfidence,space.CurrentSource,space.LastEventAt),ct);return Map(space);}
    private async Task SaveNotify(ParkingSpace space,CancellationToken ct){await db.SaveChangesAsync(ct);await notifier.ParkingChangedAsync(new(space.Id,space.Code,space.CurrentState.ToString(),space.CurrentConfidence,space.CurrentSource,space.LastEventAt),ct);}
    private static ParkingSpaceDto Map(ParkingSpace x)=>new(x.Id,x.Code,x.Zone,x.IsAccessible,x.CurrentState.ToString(),x.CurrentConfidence,x.CurrentSource,x.LastEventAt);
    private static ParkingOverviewDto Overview(IReadOnlyCollection<ParkingSpace> spaces)=>new(spaces.Count,spaces.Count(x=>x.CurrentState==ParkingState.Available),spaces.Count(x=>x.CurrentState==ParkingState.Occupied),spaces.Count(x=>x.CurrentState==ParkingState.Unknown),spaces.Count(x=>x.CurrentState==ParkingState.Disabled),spaces.Select(Map).ToList());
}
