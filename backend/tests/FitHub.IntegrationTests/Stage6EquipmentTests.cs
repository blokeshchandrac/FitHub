using FitHub.Application.Equipment;
using FitHub.Application.FaultReports;
using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Attendance;
using FitHub.Domain.Memberships;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
namespace FitHub.IntegrationTests;

public sealed class Stage6EquipmentTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine").WithDatabase("fithub_stage6_tests").WithUsername("stage6").WithPassword("Test-only-password-42!").Build();
    private WebApplicationFactory<Program> factory = null!; private RecordingNotifier notifier = null!;
    public async Task InitializeAsync()
    {
        await postgres.StartAsync(); notifier = new RecordingNotifier();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => { builder.UseEnvironment("Testing"); builder.UseSetting("ConnectionStrings:FitHubDatabase", postgres.GetConnectionString()); builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long"); builder.UseSetting("SeedData:Enabled", "false"); builder.UseSetting("EquipmentDevices:CardUidPepper", "stage6-test-pepper"); builder.ConfigureServices(services => { services.RemoveAll<IEquipmentRealtimeNotifier>(); services.AddSingleton<IEquipmentRealtimeNotifier>(notifier); services.RemoveAll<IPhotoStorage>(); services.AddSingleton<IPhotoStorage, MemoryPhotoStorage>(); }); });
        await using var scope=factory.Services.CreateAsyncScope(); await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.MigrateAsync();
    }
    public async Task DisposeAsync(){await factory.DisposeAsync();await postgres.DisposeAsync();}

    [Fact] public async Task SimultaneousClaimAllowsExactlyOneMember()
    {
        var id=await AddEquipment(); var firstUser=await AddUser(); var secondUser=await AddUser(); await using var first=factory.Services.CreateAsyncScope(); await using var second=factory.Services.CreateAsyncScope(); var gate=new TaskCompletionSource();
        var one=Task.Run(async()=>{await gate.Task;return await first.ServiceProvider.GetRequiredService<IEquipmentService>().ClaimAsync(id,firstUser);}); var two=Task.Run(async()=>{await gate.Task;return await second.ServiceProvider.GetRequiredService<IEquipmentService>().ClaimAsync(id,secondUser);}); gate.SetResult();
        var results=await Task.WhenAll(Observe(one),Observe(two)); Assert.Equal(1,results.Count(x=>x is EquipmentDetailDto)); Assert.Equal(1,results.Count(x=>x is EquipmentConcurrencyException or InvalidOperationException));
    }

    [Fact] public async Task DeviceCredentialHashAuthenticatesAndRejectsInvalidSecret()
    {
        await using var scope=factory.Services.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>(); var item=new Equipment("AUTH-1","Auth machine"); db.Equipment.Add(item); var device=new EquipmentDevice(item.Id,"SIM-AUTH","pending"); device.SetCredentialHash(scope.ServiceProvider.GetRequiredService<IPasswordHasher<EquipmentDevice>>().HashPassword(device,"correct-secret")); db.EquipmentDevices.Add(device); await db.SaveChangesAsync(); var auth=scope.ServiceProvider.GetRequiredService<IDeviceAuthenticator>();
        Assert.True(await auth.AuthenticateAsync(item.Id,"SIM-AUTH","correct-secret")); Assert.False(await auth.AuthenticateAsync(item.Id,"SIM-AUTH","wrong-secret")); Assert.NotEqual("correct-secret",device.CredentialHash);
    }

    [Fact] public async Task SignalREventIsRaisedOnlyAfterCommittedChange()
    {
        var id=await AddEquipment(); var userId=await AddUser(); notifier.CommittedCheck=async equipmentId=>{await using var scope=factory.Services.CreateAsyncScope();return await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Equipment.AnyAsync(x=>x.Id==equipmentId&&x.CurrentState==EquipmentState.Occupied);};
        await using var actionScope=factory.Services.CreateAsyncScope(); await actionScope.ServiceProvider.GetRequiredService<IEquipmentService>().ClaimAsync(id,userId); Assert.True(notifier.SawCommittedChange); Assert.Contains(notifier.Changes,x=>x.EquipmentId==id&&x.State=="Occupied");
    }

    [Fact] public async Task FaultPhotoValidationAndSeriousSafetyMaintenanceAreEnforced()
    {
        var id=await AddEquipment(); var userId=await AddUser(); await using var scope=factory.Services.CreateAsyncScope(); var service=scope.ServiceProvider.GetRequiredService<IFaultReportService>();
        await Assert.ThrowsAsync<ArgumentException>(()=>service.CreateAsync(userId,new(id,"Other",null,false,null)));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.CreateAsync(userId,new(id,"BrokenPart","bad file",false,new(new MemoryStream([1]),"bad.exe","application/octet-stream",1))));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.CreateAsync(userId,new(id,"BrokenPart","too large",false,new(new MemoryStream([1]),"large.jpg","image/jpeg",5L*1024*1024+1))));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.CreateAsync(userId,new(id,"BrokenPart","spoofed",false,new(new MemoryStream([1,2,3,4,5,6,7,8]),"spoof.png","image/png",8))));
        byte[] png=[0x89,0x50,0x4e,0x47,0x0d,0x0a,0x1a,0x0a]; var report=await service.CreateAsync(userId,new(id,"SafetyConcern","Guard is loose",true,new(new MemoryStream(png),"proof.png","image/png",png.Length))); Assert.True(report.SeriousSafetyConcern); Assert.Equal(EquipmentState.Maintenance,(await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Equipment.FindAsync(id))!.CurrentState);
    }

    [Fact] public async Task LiveAccessRequiresARecentOpenCheckIn()
    {
        var userId=await AddUser(); await using var scope=factory.Services.CreateAsyncScope(); var access=scope.ServiceProvider.GetRequiredService<IEquipmentAccessService>(); var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        Assert.False(await access.IsCheckedInAsync(userId)); var attendance=new Attendance(userId,DateTimeOffset.UtcNow); db.Attendances.Add(attendance); await db.SaveChangesAsync(); Assert.True(await access.IsCheckedInAsync(userId)); attendance.CheckedOutAt=DateTimeOffset.UtcNow; await db.SaveChangesAsync(); Assert.False(await access.IsCheckedInAsync(userId));
    }

    [Fact] public async Task DeviceCardClaimRequiresMembershipAndCheckIn()
    {
        var equipmentId=await AddEquipment(); var userId=await AddUser(); await using var scope=factory.Services.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>(); var hasher=scope.ServiceProvider.GetRequiredService<ICardUidHasher>(); db.MemberAccessCards.Add(new MemberAccessCard(userId,hasher.Hash("CARD-ACCESS"))); await db.SaveChangesAsync(); var service=scope.ServiceProvider.GetRequiredService<IEquipmentService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.DeviceClaimAsync(equipmentId,"CARD-ACCESS")); var plan=new MembershipPlan{Name="Stage 6 plan",Price=10m,BillingPeriodMonths=1}; db.MembershipPlans.Add(plan); var membership=new Membership(userId,plan.Id,DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(30)); membership.Activate(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(30)); db.Memberships.Add(membership); db.Attendances.Add(new Attendance(userId,DateTimeOffset.UtcNow)); await db.SaveChangesAsync(); await service.DeviceClaimAsync(equipmentId,"CARD-ACCESS"); Assert.Equal(EquipmentState.Occupied,(await db.Equipment.FindAsync(equipmentId))!.CurrentState);
    }

    private async Task<Guid> AddEquipment(){await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();var item=new Equipment($"TEST-{Guid.NewGuid():N}","Test equipment");db.Equipment.Add(item);await db.SaveChangesAsync();return item.Id;}
    private async Task<Guid> AddUser(){await using var scope=factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<FitHubDbContext>();var id=Guid.NewGuid();db.Users.Add(new ApplicationUser{Id=id,UserName=$"{id:N}@test.local",Email=$"{id:N}@test.local",FirstName="Test",LastName="Member"});await db.SaveChangesAsync();return id;}
    private static async Task<object?> Observe(Task<EquipmentDetailDto?> task){try{return await task;}catch(Exception e){return e;}}
    private sealed class RecordingNotifier:IEquipmentRealtimeNotifier { public List<EquipmentChangedDto> Changes{get;}=[];public Func<Guid,Task<bool>>? CommittedCheck{get;set;}public bool SawCommittedChange{get;private set;}public async Task EquipmentChangedAsync(EquipmentChangedDto change,CancellationToken cancellationToken=default){Changes.Add(change);SawCommittedChange=CommittedCheck is not null&&await CommittedCheck(change.EquipmentId);} }
    private sealed class MemoryPhotoStorage:IPhotoStorage { public Task<string> SaveAsync(Stream content,string extension,CancellationToken cancellationToken=default)=>Task.FromResult($"/test/{Guid.NewGuid():N}{extension}"); }
}
