using System.Security.Cryptography;
using System.Text;
using FitHub.Application.Attendance;
using FitHub.Domain.Attendance;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FitHub.IntegrationTests;

public sealed class Stage4AttendanceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_attendance_tests")
        .WithUsername("fithub_attendance_tests")
        .WithPassword("Test-only-database-password-42!")
        .Build();
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:FitHubDatabase", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long");
            builder.UseSetting("SeedData:Enabled", "false");
        });
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task ExpiredQrTokenIsRejectedWithoutRecordingAttendance()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<FitHubDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "expired.qr@fithub.test", Email = "expired.qr@fithub.test", FirstName = "Expired", LastName = "Token" };
        Assert.True((await userManager.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update("QR Test", null, 20m, 1);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(user.Id, plan.Id, now.AddDays(-1), now.AddDays(1));
        membership.Activate(now.AddDays(-1), now.AddDays(1));
        const string rawToken = "expired-stage4-qr-token";
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var token = new AttendanceCheckInToken(user.Id, tokenHash, now.AddMinutes(-1));
        dbContext.AddRange(plan, membership, token);
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            services.GetRequiredService<IAttendanceService>().CheckInAsync(rawToken));

        Assert.Contains("expired", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await dbContext.Attendances.ToListAsync());
    }
}
