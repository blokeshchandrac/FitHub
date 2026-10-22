using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using FitHub.Api.Simulation;
using FitHub.Application.Equipment;
using FitHub.Application.Parking;
using FitHub.Domain.Equipment;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Domain.Parking;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace FitHub.IntegrationTests;

public sealed class SimulationApiFixture : IAsyncLifetime
{
    public const string Password = "Test-only-password-42!";
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_simulation_tests")
        .WithUsername("simulation")
        .WithPassword(Password)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "simulation", "-d", "fithub_simulation_tests"))
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public SimulationMemberResponse ExistingMember { get; private set; } = null!;
    public Guid AdministratorId { get; private set; }
    public RecordingEquipmentNotifier Notifier { get; } = new();
    public RecordingParkingSimulationNotifier ParkingNotifier { get; } = new();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        Factory = CreateFactory(enabled: true, clearanceSeconds: 1);
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        await db.Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var role in new[] { "Member", "Administrator" })
            Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>(role))).Succeeded);

        var member = await AddUser(userManager, "simulation-member@tests.invalid", "Existing", "Member", "Member");
        AdministratorId = (await AddUser(userManager, "simulation-admin@tests.invalid", "Simulation", "Admin", "Administrator")).Id;
        var plan = new MembershipPlan { Name = "Simulation test", Description = "Test only", Price = 20m, BillingPeriodMonths = 1 };
        db.MembershipPlans.Add(plan);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(member.Id, plan.Id, now.AddDays(-1), now.AddMonths(1));
        membership.Activate(now.AddDays(-1), now.AddMonths(1));
        db.Memberships.Add(membership);
        foreach (var item in new[]
        {
            new Equipment("TREAD-001", "Treadmill"),
            new Equipment("RACK-001", "Power rack"),
            new Equipment("BIKE-001", "Exercise bike"),
            new Equipment("CABLE-001", "Cable machine")
        })
        {
            db.Equipment.Add(item);
            db.EquipmentDevices.Add(new EquipmentDevice(item.Id, $"TEST-{item.Code}", "test-only-hash"));
        }
        db.ParkingSpaces.AddRange(Enumerable.Range(0, 3)
            .SelectMany(row => Enumerable.Range(1, 4)
                .Select(number => new ParkingSpace($"P-{(char)('A' + row)}{number}", "Simulation"))));
        db.ParkingCameras.Add(new ParkingCamera(ParkingSimulationConstants.CameraId, "test-only-hash"));
        await db.SaveChangesAsync();

        using var client = Factory.CreateClient();
        ExistingMember = Assert.Single((await client.GetFromJsonAsync<SimulationMemberResponse[]>("/api/simulation/members"))!);
    }

    public WebApplicationFactory<Program> CreateFactory(bool enabled, int clearanceSeconds) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:FitHubDatabase", postgres.GetConnectionString());
            builder.UseSetting("Simulation:Enabled", enabled.ToString());
            builder.UseSetting("Simulation:ClearanceSeconds", clearanceSeconds.ToString());
            builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long");
            builder.UseSetting("SeedData:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEquipmentRealtimeNotifier>();
                services.AddSingleton<IEquipmentRealtimeNotifier>(Notifier);
                services.RemoveAll<IParkingRealtimeNotifier>();
                services.AddSingleton<IParkingRealtimeNotifier>(ParkingNotifier);
            });
        });

    public async Task<HttpClient> CreateAdministratorClientAsync()
    {
        var client = Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "simulation-admin@tests.invalid", password = Password });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    public async Task ResetEquipmentAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        foreach (var item in await db.Equipment.Include(x => x.StatusHistory).ToListAsync())
            item.AdministratorOverride(EquipmentState.Available, DateTimeOffset.UtcNow, AdministratorId, "Test reset");
        await db.SaveChangesAsync();
        Notifier.Changes.Clear();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await postgres.DisposeAsync();
    }

    private static async Task<ApplicationUser> AddUser(UserManager<ApplicationUser> manager, string email, string first, string last, string role)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, FirstName = first, LastName = last, IsActive = true };
        Assert.True((await manager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await manager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }
}

public sealed class SimulationApiTests(SimulationApiFixture fixture) : IClassFixture<SimulationApiFixture>
{
    [Fact]
    public async Task ConnectedEndpointsRequireAdministratorAndDisappearWhenDisabled()
    {
        using var anonymous = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/simulation/status")).StatusCode);
        await using var disabledFactory = fixture.CreateFactory(enabled: false, clearanceSeconds: 30);
        using var admin = await AdministratorClient(disabledFactory);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/simulation/status")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/simulation/equipment")).StatusCode);
    }

    [Fact]
    public void MemberCatalogueReturnsOnlyMinimalDatabaseBackedInformation()
    {
        Assert.Equal("Existing Member", fixture.ExistingMember.DisplayName);
        Assert.Equal("ACTIVE", fixture.ExistingMember.MembershipStatus);
        Assert.Equal("ACTIVE", fixture.ExistingMember.CardStatus);
        Assert.Equal("SIMULATION_TOKEN", fixture.ExistingMember.CardSource);
        Assert.DoesNotContain("@", fixture.ExistingMember.DisplayName);
    }

    [Fact]
    public async Task ConnectedClaimPersistsAndPublishesThroughNormalEquipmentNotifier()
    {
        await fixture.ResetEquipmentAsync();
        using var client = await fixture.CreateAdministratorClientAsync();
        const string session = "SIM-PERSISTED-CLAIM";
        Assert.True((await Send(client, "ENTRANCE_SCAN", "ENTRANCE-01", "SIM-ENTRY-01", fixture.ExistingMember, session)).Allowed);
        var result = await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", fixture.ExistingMember, session);
        Assert.Equal("OCCUPIED", result.EquipmentState);
        Assert.NotNull(result.PersistedEquipmentId);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        Assert.Equal(EquipmentState.Occupied, (await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Equipment.SingleAsync(x => x.Code == "TREAD-001")).CurrentState);
        Assert.Contains(fixture.Notifier.Changes, change => change.EquipmentId == result.PersistedEquipmentId && change.State == "Occupied");
    }

    [Fact]
    public async Task ConnectedSharingAllowsOnePartnerAndRejectsAThirdMember()
    {
        await fixture.ResetEquipmentAsync();
        var second = await AddActiveMember("second-simulation@tests.invalid", "Second Member");
        var third = await AddActiveMember("third-simulation@tests.invalid", "Third Member");
        using var client = await fixture.CreateAdministratorClientAsync();
        const string session = "SIM-SHARING";
        foreach (var member in new[] { fixture.ExistingMember, second, third })
            Assert.True((await Send(client, "ENTRANCE_SCAN", "ENTRANCE-01", "SIM-ENTRY-01", member, session)).Allowed);
        Assert.Equal("OCCUPIED", (await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", fixture.ExistingMember, session)).EquipmentState);
        Assert.Equal("SHARING_ALLOWED", (await Send(client, "SHARING_ENABLED", "EQ-TREADMILL-01", "SIM-ESP32-01", fixture.ExistingMember, session)).EquipmentState);
        Assert.Equal("SECOND_MEMBER_JOINED", (await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", second, session)).Code);
        var rejected = await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", third, session);
        Assert.False(rejected.Allowed);
        Assert.Equal("SHARING_FULL", rejected.Code);
        Assert.Equal(fixture.ExistingMember.MemberId, rejected.OwnerMemberId);
        Assert.Equal(second.MemberId, rejected.PartnerMemberId);
    }

    [Fact]
    public async Task ConnectedClearanceIgnoresDuplicateAndStaleEventsThenPersistsAndPublishesRelease()
    {
        await fixture.ResetEquipmentAsync();
        var second = await AddActiveMember("clearance-partner@tests.invalid", "Clearance Partner");
        using var client = await fixture.CreateAdministratorClientAsync();
        const string session = "SIM-TIMER-SESSION";
        foreach (var member in new[] { fixture.ExistingMember, second })
            Assert.True((await Send(client, "ENTRANCE_SCAN", "ENTRANCE-01", "SIM-ENTRY-01", member, session)).Allowed);
        await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", fixture.ExistingMember, session);
        await Send(client, "SHARING_ENABLED", "EQ-TREADMILL-01", "SIM-ESP32-01", fixture.ExistingMember, session);
        await Send(client, "NFC_TAPPED", "EQ-TREADMILL-01", "SIM-ESP32-01", second, session);

        var firstExitAt = DateTimeOffset.UtcNow.AddSeconds(-2);
        var detected = await Send(client, "SENSOR_ZONE_ENTERED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt.AddSeconds(-1));
        Assert.Equal("SENSOR_ZONE_ENTERED", detected.Code);
        Assert.Equal("SHARING_ALLOWED", detected.EquipmentState);
        var started = await Send(client, "SENSOR_ZONE_EXITED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt);
        Assert.Equal("CLEARANCE_COUNTDOWN_STARTED", started.Code);
        Assert.Equal("CLEARANCE_WARNING", started.EquipmentState);

        var duplicate = await Send(client, "SENSOR_ZONE_EXITED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt.AddMilliseconds(500));
        Assert.Equal("SENSOR_OBSERVATION_IGNORED", duplicate.Code);
        Assert.Equal("CLEARANCE_WARNING", duplicate.EquipmentState);
        var staleReturn = await Send(client, "SENSOR_ZONE_ENTERED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt.AddMilliseconds(250));
        Assert.False(staleReturn.Allowed);
        Assert.Equal("STALE_SENSOR_EVENT", staleReturn.Code);
        Assert.Equal("CLEARANCE_WARNING", staleReturn.EquipmentState);

        var cancelled = await Send(client, "SENSOR_ZONE_ENTERED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt.AddMilliseconds(750));
        Assert.Equal("CLEARANCE_COUNTDOWN_CANCELLED", cancelled.Code);
        Assert.Equal("SHARING_ALLOWED", cancelled.EquipmentState);

        var secondExit = await Send(client, "SENSOR_ZONE_EXITED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session, observedAt: firstExitAt.AddSeconds(1));
        Assert.Equal("CLEARANCE_COUNTDOWN_STARTED", secondExit.Code);
        await Task.Delay(1100);
        var completed = await Send(client, "AUTOMATIC_RELEASE_COMPLETED", "EQ-TREADMILL-01", "SIM-ESP32-01", null, session);
        Assert.Equal("AVAILABLE", completed.EquipmentState);
        Assert.Null(completed.OwnerMemberId);
        Assert.Null(completed.PartnerMemberId);

        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Equipment
            .Include(item => item.StatusHistory).SingleAsync(item => item.Code == "TREAD-001");
        Assert.Equal(EquipmentState.Available, persisted.CurrentState);
        Assert.Null(persisted.OwnerUserId);
        Assert.Null(persisted.PartnerUserId);
        Assert.Equal(2, persisted.StatusHistory.Count(item => item.EventType == EquipmentEventType.ClearanceWarningStarted));
        Assert.Single(persisted.StatusHistory, item => item.EventType == EquipmentEventType.ClearanceWarningCancelled);
        Assert.Single(persisted.StatusHistory, item => item.EventType == EquipmentEventType.ClearanceConfirmed);
        Assert.Contains(fixture.Notifier.Changes, change => change.EquipmentId == persisted.Id && change.State == "ClearanceWarning");
        Assert.Contains(fixture.Notifier.Changes, change => change.EquipmentId == persisted.Id && change.State == "SharingAllowed");
        Assert.Contains(fixture.Notifier.Changes, change => change.EquipmentId == persisted.Id && change.State == "Available");
    }

    [Fact]
    public async Task SessionOverrideChangesDecisionWithoutChangingMembershipRecord()
    {
        using var client = await fixture.CreateAdministratorClientAsync();
        var denied = await Send(client, "ENTRANCE_SCAN", "ENTRANCE-01", "SIM-ENTRY-01", fixture.ExistingMember,
            "SIM-OVERRIDE", SimulationStatusOverride.ExpiredMembership);
        Assert.Equal("MEMBERSHIP_EXPIRED", denied.Code);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        Assert.Equal(MembershipStatus.Active, (await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Memberships.FirstAsync()).Status);
    }

    [Fact]
    public async Task ConnectedParkingScanPersistsAndPublishesThroughNormalParkingNotifier()
    {
        using var client = await fixture.CreateAdministratorClientAsync();
        var observations = Enumerable.Range(0, 3)
            .SelectMany(row => Enumerable.Range(1, 4).Select(number => new
            {
                spaceId = $"P-{(char)('A' + row)}{number}",
                status = row == 0 && number is 2 or 3 ? "MISUSED_OR_BLOCKED" : "AVAILABLE",
                confidence = .94m
            })).ToArray();
        var request = new
        {
            simulationSessionId = "SIM-PARK-INTEGRATED",
            sourceDeviceId = ParkingSimulationConstants.CameraId,
            observedAt = DateTimeOffset.UtcNow,
            observations,
            violations = new[]
            {
                new { vehicleSimulationId = "Car 01", affectedSpaceIds = new[] { "P-A2", "P-A3" }, violationType = "TWO_SPACE_OBSTRUCTION", reason = "Car 01 obstructs P-A2 and P-A3." }
            }
        };
        using var response = await client.PostAsJsonAsync("/api/simulation/parking/scans", request);
        response.EnsureSuccessStatusCode();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        Assert.Equal(ParkingState.MisusedOrBlocked, (await db.ParkingSpaces.SingleAsync(item => item.Code == "P-A2")).CurrentState);
        Assert.Single(await db.ParkingViolationAlerts.Where(item => item.IsActive).ToListAsync());
        Assert.Contains(fixture.ParkingNotifier.SpaceChanges, item => item.Code == "P-A2" && item.State == "MisusedOrBlocked");
        Assert.Single(fixture.ParkingNotifier.AlertChanges, item => item.Status == "ACTIVE");
    }

    private async Task<SimulationMemberResponse> AddActiveMember(string email, string name)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, FirstName = name.Split(' ')[0], LastName = name.Split(' ')[1], IsActive = true };
        Assert.True((await manager.CreateAsync(user, SimulationApiFixture.Password)).Succeeded);
        Assert.True((await manager.AddToRoleAsync(user, "Member")).Succeeded);
        var plan = await db.MembershipPlans.FirstAsync();
        var membership = new Membership(user.Id, plan.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        membership.Activate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();
        using var anonymous = fixture.Factory.CreateClient();
        return (await anonymous.GetFromJsonAsync<SimulationMemberResponse[]>("/api/simulation/members"))!.Single(item => item.MemberId == user.Id);
    }

    private static async Task<HttpClient> AdministratorClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "simulation-admin@tests.invalid", password = SimulationApiFixture.Password });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<SimulationEventResponse> Send(
        HttpClient client,
        string eventType,
        string equipmentId,
        string deviceId,
        SimulationMemberResponse? member,
        string sessionId,
        SimulationStatusOverride statusOverride = SimulationStatusOverride.Actual,
        DateTimeOffset? observedAt = null)
    {
        var request = new SimulationEventRequest(sessionId, deviceId, equipmentId, eventType, member?.CardId,
            null, observedAt ?? DateTimeOffset.UtcNow, true, statusOverride, member?.MemberId);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var response = await client.PostAsJsonAsync("/api/simulation/events", request, options);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Expected success but received {(int)response.StatusCode}: {body}");
        return (await response.Content.ReadFromJsonAsync<SimulationEventResponse>())!;
    }
}

public sealed class RecordingEquipmentNotifier : IEquipmentRealtimeNotifier
{
    public List<EquipmentChangedDto> Changes { get; } = [];
    public Task EquipmentChangedAsync(EquipmentChangedDto change, CancellationToken cancellationToken = default)
    {
        Changes.Add(change);
        return Task.CompletedTask;
    }
}

public sealed class RecordingParkingSimulationNotifier : IParkingRealtimeNotifier
{
    public List<ParkingChangedDto> SpaceChanges { get; } = [];
    public List<ParkingAlertChangedDto> AlertChanges { get; } = [];
    public Task ParkingChangedAsync(ParkingChangedDto change, CancellationToken cancellationToken = default)
    {
        SpaceChanges.Add(change);
        return Task.CompletedTask;
    }
    public Task ParkingAlertChangedAsync(ParkingAlertChangedDto change, CancellationToken cancellationToken = default)
    {
        AlertChanges.Add(change);
        return Task.CompletedTask;
    }
}
