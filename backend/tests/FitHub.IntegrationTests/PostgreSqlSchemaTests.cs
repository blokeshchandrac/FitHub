using FitHub.Infrastructure.Persistence;
using FitHub.Infrastructure;
using FitHub.Infrastructure.Persistence.Seeding;
using FitHub.Domain.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FitHub.IntegrationTests;

public sealed class PostgreSqlSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_tests")
        .WithUsername("fithub_tests")
        .WithPassword("Test-only-password-42!")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task InitialMigrationCreatesPostgreSqlSchema()
    {
        var options = new DbContextOptionsBuilder<FitHubDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var dbContext = new FitHubDbContext(options);
        await dbContext.Database.MigrateAsync();

        Assert.True(await dbContext.Database.CanConnectAsync());
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
        _ = await dbContext.Users.CountAsync();
        _ = await dbContext.Equipment.CountAsync();
        _ = await dbContext.ParkingSpaces.CountAsync();
    }

    [Fact]
    public async Task EquipmentStatusUsesOptimisticConcurrency()
    {
        var options = new DbContextOptionsBuilder<FitHubDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var setupContext = new FitHubDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
            setupContext.Users.AddRange(
                new FitHub.Domain.Identity.ApplicationUser { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), UserName = "claim@test.local", Email = "claim@test.local", FirstName = "Claim", LastName = "User" },
                new FitHub.Domain.Identity.ApplicationUser { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), UserName = "admin@test.local", Email = "admin@test.local", FirstName = "Admin", LastName = "User" });
            setupContext.Equipment.Add(new Equipment($"TEST-{Guid.NewGuid():N}", "Concurrency test equipment"));
            await setupContext.SaveChangesAsync();
        }

        await using var firstContext = new FitHubDbContext(options);
        await using var secondContext = new FitHubDbContext(options);
        var firstCopy = await firstContext.Equipment.OrderByDescending(item => item.CreatedAt).FirstAsync();
        var secondCopy = await secondContext.Equipment.SingleAsync(item => item.Id == firstCopy.Id);

        firstCopy.Claim(Guid.Parse("10000000-0000-0000-0000-000000000001"), DateTimeOffset.UtcNow);
        await firstContext.SaveChangesAsync();

        secondCopy.AdministratorOverride(EquipmentState.Maintenance, DateTimeOffset.UtcNow.AddSeconds(1), Guid.Parse("10000000-0000-0000-0000-000000000002"), "Concurrency test");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task DevelopmentSeederCreatesRolesUsersAndReferenceData()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FitHubDatabase"] = _postgres.GetConnectionString(),
                ["SeedData:DemoPassword"] = "Test-only-demo-password-42!",
                ["SeedData:Accounts:0:Email"] = "member@fithub.test",
                ["SeedData:Accounts:0:FirstName"] = "Morgan",
                ["SeedData:Accounts:0:LastName"] = "Member",
                ["SeedData:Accounts:0:Role"] = "Member",
                ["SeedData:Accounts:1:Email"] = "trainer@fithub.test",
                ["SeedData:Accounts:1:FirstName"] = "Taylor",
                ["SeedData:Accounts:1:LastName"] = "Trainer",
                ["SeedData:Accounts:1:Role"] = "Trainer",
                ["SeedData:Accounts:2:Email"] = "admin@fithub.test",
                ["SeedData:Accounts:2:FirstName"] = "Ada",
                ["SeedData:Accounts:2:LastName"] = "Admin",
                ["SeedData:Accounts:2:Role"] = "Administrator"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();

        var dbContext = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Equal(3, await dbContext.Users.CountAsync());
        Assert.Equal(2, await dbContext.MembershipPlans.CountAsync());
        Assert.Equal(3, await dbContext.Exercises.CountAsync());
        Assert.Equal(3, await dbContext.Equipment.CountAsync());
        Assert.Equal(3, await dbContext.ParkingSpaces.CountAsync());
    }
}
