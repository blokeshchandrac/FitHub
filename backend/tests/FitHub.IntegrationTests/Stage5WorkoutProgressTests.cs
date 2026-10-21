using FitHub.Application.Dashboard;
using FitHub.Application.Progress;
using FitHub.Application.Workouts;
using FitHub.Domain.Identity;
using FitHub.Domain.Workouts;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FitHub.IntegrationTests;

public sealed class Stage5WorkoutProgressTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_stage5_tests")
        .WithUsername("fithub_stage5_tests")
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
    public async Task WorkoutProgressAndDashboardQueriesUsePersistedData()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<FitHubDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "stage5@fithub.test", Email = "stage5@fithub.test", FirstName = "Stage", LastName = "Five" };
        Assert.True((await userManager.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var exercise = new Exercise { Name = "Stage 5 Press", MuscleGroup = "Chest", Equipment = "Barbell", Instructions = "Press safely." };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        var workouts = services.GetRequiredService<IWorkoutService>();
        var template = await workouts.CreateTemplateAsync(user.Id, new UpsertWorkoutTemplateCommand("Strength", null, false, [exercise.Id]));
        var session = await workouts.StartSessionAsync(user.Id, template.Id);
        session = (await workouts.LogExerciseAsync(user.Id, session.Id, new LogExerciseCommand(exercise.Id, null,
            [new WorkoutSetCommand(10, 50m, 45, 90), new WorkoutSetCommand(8, 60m, null, null)])))!;
        var completed = await workouts.CompleteSessionAsync(user.Id, session.Id, null);
        var persisted = await workouts.GetSessionAsync(user.Id, session.Id);
        var nextSession = await workouts.StartSessionAsync(user.Id, template.Id);

        var progress = services.GetRequiredService<IProgressService>();
        await progress.LogMeasurementAsync(user.Id, new BodyMeasurementCommand(81m, 180m, 20m, null, 85m, null, null));
        var bodyProgress = await progress.GetProgressAsync(user.Id);
        var dashboard = await services.GetRequiredService<IDashboardService>().GetDashboardAsync(user.Id);

        Assert.Equal(980m, completed!.Volume);
        var firstSet = Assert.IsType<WorkoutSetDto>(persisted!.Exercises.Single().Sets.First());
        Assert.Equal(45, firstSet.DurationSeconds);
        Assert.Equal(90, firstSet.RestSeconds);
        Assert.Equal(500m, firstSet.Volume);
        var storedSet = await dbContext.SetLogs.AsNoTracking().SingleAsync(item => item.Id == firstSet.Id);
        Assert.Equal(90, storedSet.RestSeconds);
        Assert.Equal(60m, nextSession.Exercises.Single().PreviousPerformance!.BestWeightKg);
        Assert.Equal(25m, bodyProgress.Bmi);
        Assert.Equal(980m, dashboard.WeeklyVolume);
        Assert.Contains(dashboard.PersonalRecords, record => record.ExerciseId == exercise.Id && record.WeightKg == 60m);
    }
}
