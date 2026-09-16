using FitHub.Application.Parking;using FitHub.Application.Recommendations;using FitHub.Domain.Identity;using FitHub.Domain.Parking;using FitHub.Domain.Progress;using FitHub.Infrastructure.Persistence;using Microsoft.AspNetCore.Hosting;using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc.Testing;using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.DependencyInjection.Extensions;using Testcontainers.PostgreSql;
namespace FitHub.IntegrationTests;
public sealed class Stage7ParkingRecommendationTests:IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres=new PostgreSqlBuilder("postgres:18-alpine").WithDatabase("fithub_stage7_tests").WithUsername("stage7").WithPassword("Test-only-password-42!").Build();private WebApplicationFactory<Program> factory=null!;private RecordingParkingNotifier notifier=null!;
    public async Task InitializeAsync(){await postgres.StartAsync();notifier=new();factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{builder.UseEnvironment("Testing");builder.UseSetting("ConnectionStrings:FitHubDatabase",postgres.GetConnectionString());builder.UseSetting("Jwt:SigningKey","FitHub-tests-only-signing-key-at-least-32-characters-long");builder.UseSetting("SeedData:Enabled","false");builder.ConfigureServices(services=>{services.RemoveAll<IParkingRealtimeNotifier>();services.AddSingleton<IParkingRealtimeNotifier>(notifier);});});await using var scope=factory.Services.CreateAsyncScope();await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.MigrateAsync();}
    public async Task DisposeAsync(){await factory.DisposeAsync();await postgres.DisposeAsync();}
    [Fact]public async Task CameraCredentialIsHashedAndDetectionBroadcastsAfterCommit(){await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();var space=new ParkingSpace("S7-01","Stage 7");var camera=new ParkingCamera("CAM-S7","pending");camera.SetCredentialHash(scope.ServiceProvider.GetRequiredService<IPasswordHasher<ParkingCamera>>().HashPassword(camera,"camera-secret"));db.ParkingSpaces.Add(space);db.ParkingCameras.Add(camera);await db.SaveChangesAsync();var auth=scope.ServiceProvider.GetRequiredService<IParkingCameraAuthenticator>();Assert.True(await auth.AuthenticateAsync("CAM-S7","camera-secret"));Assert.False(await auth.AuthenticateAsync("CAM-S7","wrong"));Assert.NotEqual("camera-secret",camera.CredentialHash);notifier.CommittedCheck=async id=>{await using var check=factory.Services.CreateAsyncScope();return await check.ServiceProvider.GetRequiredService<FitHubDbContext>().ParkingSpaces.AnyAsync(x=>x.Id==id&&x.CurrentState==ParkingState.Occupied);};var result=await scope.ServiceProvider.GetRequiredService<IParkingService>().RecordDetectionAsync("CAM-S7",new("S7-01","Occupied",.91m,"IntegrationTest",space.LastEventAt.AddSeconds(1)));Assert.Equal("Occupied",result.State);Assert.True(notifier.SawCommitted);Assert.Equal(.91m,(await db.ParkingStatusEvents.SingleAsync()).Confidence);}
    [Fact]public async Task RecommendationServicePersistsRuleAndReason(){await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();var id=Guid.NewGuid();db.Users.Add(new ApplicationUser{Id=id,UserName="stage7@test.local",Email="stage7@test.local",FirstName="Stage",LastName="Seven"});db.BodyMeasurements.Add(new BodyMeasurement{UserId=id,RecordedAt=DateTimeOffset.UtcNow,WeightKg=75m});await db.SaveChangesAsync();var result=await scope.ServiceProvider.GetRequiredService<IRecommendationService>().GenerateAsync(id);Assert.NotEmpty(result.Recommendations);Assert.Contains(result.Recommendations,x=>x.RuleCode=="INSUFFICIENT_DATA"&&x.Title=="Not enough data");Assert.All(await db.Recommendations.Where(x=>x.UserId==id).ToListAsync(),x=>{Assert.NotEmpty(x.RuleCode);Assert.NotEmpty(x.Reason);});}
    [Fact]
    public async Task SimulationScanPersistsTwelveSpacesAndDeduplicatesThenResolvesAlert()
    {
        await using var scope=factory.Services.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        var at=DateTimeOffset.UtcNow.AddSeconds(1);
        var codes=Enumerable.Range(0,3).SelectMany(row=>Enumerable.Range(1,4).Select(number=>$"P-{(char)('A'+row)}{number}")).ToArray();
        db.ParkingSpaces.AddRange(codes.Select(code=>new ParkingSpace(code,"Simulation")));
        db.ParkingCameras.Add(new ParkingCamera(ParkingSimulationConstants.CameraId,"test-only-hash"));
        await db.SaveChangesAsync();
        var service=scope.ServiceProvider.GetRequiredService<IParkingSimulationService>();
        var observations=codes.Select(code=>new ParkingSimulationObservationCommand(code,code is "P-A2" or "P-A3"?"MISUSED_OR_BLOCKED":"AVAILABLE",.92m)).ToArray();
        var violation=new ParkingSimulationViolationCommand("Car 01",["P-A2","P-A3"],"TWO_SPACE_OBSTRUCTION","Car 01 obstructs two spaces.");

        var first=await service.RecordScanAsync(new("SIM-TEST",ParkingSimulationConstants.CameraId,at,observations,[violation]));
        var repeated=await service.RecordScanAsync(new("SIM-TEST",ParkingSimulationConstants.CameraId,at.AddSeconds(1),observations,[violation]));
        var resolved=await service.RecordScanAsync(new("SIM-TEST",ParkingSimulationConstants.CameraId,at.AddSeconds(2),codes.Select(code=>new ParkingSimulationObservationCommand(code,"AVAILABLE",.98m)).ToArray(),[]));

        Assert.Equal(1,first.CreatedAlerts);
        Assert.Equal(0,repeated.CreatedAlerts);
        Assert.Single(await db.ParkingViolationAlerts.ToListAsync());
        Assert.Equal(1,resolved.ResolvedAlerts);
        Assert.False((await db.ParkingViolationAlerts.SingleAsync()).IsActive);
        Assert.Equal(ParkingState.Available,(await db.ParkingSpaces.SingleAsync(x=>x.Code=="P-A2")).CurrentState);
    }

    private sealed class RecordingParkingNotifier:IParkingRealtimeNotifier{public Func<Guid,Task<bool>>? CommittedCheck{get;set;}public bool SawCommitted{get;private set;}public async Task ParkingChangedAsync(ParkingChangedDto change,CancellationToken ct=default){SawCommitted=CommittedCheck is not null&&await CommittedCheck(change.SpaceId);}}
}
