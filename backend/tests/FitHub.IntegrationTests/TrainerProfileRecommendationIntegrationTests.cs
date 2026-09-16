using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitHub.Application.Authentication;
using FitHub.Application.Trainers;
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

public sealed class TrainerProfileRecommendationIntegrationTests : IAsyncLifetime
{
    private const string Password = "Test-only-Password-42!";
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_trainer_profile_tests")
        .WithUsername("trainer_profile_tests")
        .WithPassword("Test-only-database-password-42!")
        .Build();
    private WebApplicationFactory<Program> factory = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:FitHubDatabase", postgres.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long");
            builder.UseSetting("SeedData:Enabled", "false");
        });
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { AuthorizationPolicies.Member, AuthorizationPolicies.Trainer, AuthorizationPolicies.Administrator })
            Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(role))).Succeeded);
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await postgres.DisposeAsync();
    }

    [Fact]
    public async Task AdministratorAssignmentsRejectDuplicatesAndDisabledAccountsAndPreserveHistory()
    {
        var admin = await CreateUserAsync("assignment-admin@test.local", AuthorizationPolicies.Administrator);
        var trainer = await CreateUserAsync("assignment-trainer@test.local", AuthorizationPolicies.Trainer);
        var disabledTrainer = await CreateUserAsync("disabled-trainer@test.local", AuthorizationPolicies.Trainer, false);
        var member = await CreateUserAsync("assignment-member@test.local", AuthorizationPolicies.Member);
        var disabledMember = await CreateUserAsync("disabled-member@test.local", AuthorizationPolicies.Member, false);
        using var client = CreateClient();
        var token = await LoginAsync(client, admin.Email!);

        using var created = await SendJsonAsync(client, HttpMethod.Post,
            $"/api/administration/trainers/{trainer.Id}/assignments", token, new { memberId = member.Id });
        using var duplicate = await SendJsonAsync(client, HttpMethod.Post,
            $"/api/administration/trainers/{trainer.Id}/assignments", token, new { memberId = member.Id });
        using var trainerDisabled = await SendJsonAsync(client, HttpMethod.Post,
            $"/api/administration/trainers/{disabledTrainer.Id}/assignments", token, new { memberId = member.Id });
        using var memberDisabled = await SendJsonAsync(client, HttpMethod.Post,
            $"/api/administration/trainers/{trainer.Id}/assignments", token, new { memberId = disabledMember.Id });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, trainerDisabled.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, memberDisabled.StatusCode);
        var payload = await created.Content.ReadFromJsonAsync<AssignmentPayload>();
        Assert.NotNull(payload);
        using var ended = await SendAsync(client, HttpMethod.Delete,
            $"/api/administration/trainers/{trainer.Id}/assignments/{payload.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, ended.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        var stored = await db.TrainerMemberAssignments.SingleAsync(item => item.Id == payload.Id);
        Assert.Equal(TrainerMemberAssignmentStatus.Ended, stored.Status);
        Assert.NotNull(stored.UnassignedAt);
        Assert.Equal(2, await db.AuditLogs.CountAsync(item => item.EntityId == stored.Id));
        db.TrainerMemberAssignments.AddRange(
            new TrainerMemberAssignment(trainer.Id, member.Id, admin.Id, DateTimeOffset.UtcNow),
            new TrainerMemberAssignment(trainer.Id, member.Id, admin.Id, DateTimeOffset.UtcNow.AddSeconds(1)));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var trainerToken = await LoginAsync(client, trainer.Email!);
        Assert.Empty(await GetAsync<AssignedMemberPayload[]>(client, "/api/trainer/members", trainerToken));
    }

    [Fact]
    public async Task TrainerPlansAndMemberDataAreRestrictedToAssignedMembersAndPlanOwner()
    {
        var admin = await CreateUserAsync("plan-admin@test.local", AuthorizationPolicies.Administrator);
        var trainer = await CreateUserAsync("plan-trainer@test.local", AuthorizationPolicies.Trainer);
        var otherTrainer = await CreateUserAsync("plan-other-trainer@test.local", AuthorizationPolicies.Trainer);
        var assignedMember = await CreateUserAsync("plan-assigned@test.local", AuthorizationPolicies.Member);
        var unassignedMember = await CreateUserAsync("plan-unassigned@test.local", AuthorizationPolicies.Member);
        Guid exerciseId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
            var branch = new GymBranch("TEST", "Test Branch");
            db.GymBranches.Add(branch);
            var exercise = new Exercise { Name = "Integration press", MuscleGroup = "Chest", Equipment = "Bench" };
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();
            exerciseId = exercise.Id;
            var assignmentService = scope.ServiceProvider.GetRequiredService<ITrainerMemberAssignmentService>();
            await assignmentService.AssignAsync(admin.Id, trainer.Id, assignedMember.Id);
            await assignmentService.AssignAsync(admin.Id, otherTrainer.Id, unassignedMember.Id);
            db.WorkoutTemplates.Add(new WorkoutTemplate
            {
                OwnerUserId = assignedMember.Id,
                Name = "Private member template",
                IsPublic = false,
                Kind = WorkoutTemplateKind.Personal
            });
            await db.SaveChangesAsync();
        }

        using var client = CreateClient();
        var trainerToken = await LoginAsync(client, trainer.Email!);
        var otherTrainerToken = await LoginAsync(client, otherTrainer.Email!);
        using var created = await SendJsonAsync(client, HttpMethod.Post, "/api/trainer/workout-plans", trainerToken,
            new { name = "Assigned strength plan", description = "Controlled trainer plan", isPublic = true, exerciseIds = new[] { exerciseId } });
        using var otherCreated = await SendJsonAsync(client, HttpMethod.Post, "/api/trainer/workout-plans", otherTrainerToken,
            new { name = "Other trainer plan", description = "Private owner", isPublic = false, exerciseIds = new[] { exerciseId } });
        var plan = await created.Content.ReadFromJsonAsync<PlanPayload>();
        var otherPlan = await otherCreated.Content.ReadFromJsonAsync<PlanPayload>();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("TrainerPlan", plan!.Kind);
        Assert.False(plan.IsPublic);

        using var editOther = await SendJsonAsync(client, HttpMethod.Put, $"/api/trainer/workout-plans/{otherPlan!.Id}", trainerToken,
            new { name = "Tampered", description = "Must fail", isPublic = false, exerciseIds = new[] { exerciseId } });
        using var assignAllowed = await SendJsonAsync(client, HttpMethod.Post, "/api/trainer/workout-assignments", trainerToken,
            new { templateId = plan.Id, memberUserId = assignedMember.Id });
        using var assignDenied = await SendJsonAsync(client, HttpMethod.Post, "/api/trainer/workout-assignments", trainerToken,
            new { templateId = plan.Id, memberUserId = unassignedMember.Id });
        using var directProfileDenied = await SendAsync(client, HttpMethod.Get,
            $"/api/trainer/members/{unassignedMember.Id}/profile", trainerToken);

        Assert.Equal(HttpStatusCode.NotFound, editOther.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assignAllowed.StatusCode);
        var workoutAssignment = await assignAllowed.Content.ReadFromJsonAsync<AssignmentPayload>();
        Assert.Equal(HttpStatusCode.Forbidden, assignDenied.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, directProfileDenied.StatusCode);
        var members = await GetAsync<AssignedMemberPayload[]>(client, "/api/trainer/members", trainerToken);
        Assert.Single(members);
        Assert.Equal(assignedMember.Id, members[0].Id);
        var plans = await GetAsync<PlanPayload[]>(client, "/api/trainer/workout-plans", trainerToken);
        Assert.DoesNotContain(plans, item => item.Name == "Private member template");
        using var removed = await SendAsync(client, HttpMethod.Delete,
            $"/api/trainer/workout-assignments/{workoutAssignment!.Id}", trainerToken);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact]
    public async Task ProfileAcceptsControlledTrainingFieldsAndRejectsInvalidOrInactiveBranches()
    {
        var member = await CreateUserAsync("profile-member@test.local", AuthorizationPolicies.Member);
        Guid activeBranchId;
        Guid inactiveBranchId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
            var active = new GymBranch("ACTIVE", "Active Test Branch");
            var inactive = new GymBranch("CLOSED", "Inactive Test Branch", false);
            db.GymBranches.AddRange(active, inactive);
            await db.SaveChangesAsync();
            activeBranchId = active.Id;
            inactiveBranchId = inactive.Id;
        }
        using var client = CreateClient();
        var token = await LoginAsync(client, member.Email!);
        var valid = new
        {
            preferredName = "Profile Member", dateOfBirth = "1995-06-15", heightCm = 175.5m, weightKg = 76.2m,
            fitnessGoal = "Strength", activityLevel = "Moderate", experienceLevel = "Intermediate",
            plannedTrainingDays = new[] { "Monday", "Thursday" }, preferredGymBranchId = activeBranchId
        };
        using var accepted = await SendJsonAsync(client, HttpMethod.Put, "/api/profile", token, valid);
        using var invalidExperience = await SendJsonAsync(client, HttpMethod.Put, "/api/profile", token,
            new { valid.preferredName, valid.dateOfBirth, valid.heightCm, valid.weightKg, valid.fitnessGoal, valid.activityLevel, experienceLevel = "Expert", valid.plannedTrainingDays, valid.preferredGymBranchId });
        using var emptyDays = await SendJsonAsync(client, HttpMethod.Put, "/api/profile", token,
            new { valid.preferredName, valid.dateOfBirth, valid.heightCm, valid.weightKg, valid.fitnessGoal, valid.activityLevel, valid.experienceLevel, plannedTrainingDays = Array.Empty<string>(), valid.preferredGymBranchId });
        using var inactiveBranch = await SendJsonAsync(client, HttpMethod.Put, "/api/profile", token,
            new { valid.preferredName, valid.dateOfBirth, valid.heightCm, valid.weightKg, valid.fitnessGoal, valid.activityLevel, valid.experienceLevel, valid.plannedTrainingDays, preferredGymBranchId = inactiveBranchId });
        using var unknownBranch = await SendJsonAsync(client, HttpMethod.Put, "/api/profile", token,
            new { valid.preferredName, valid.dateOfBirth, valid.heightCm, valid.weightKg, valid.fitnessGoal, valid.activityLevel, valid.experienceLevel, valid.plannedTrainingDays, preferredGymBranchId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidExperience.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyDays.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, inactiveBranch.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownBranch.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var stored = await verifyScope.ServiceProvider.GetRequiredService<FitHubDbContext>().Users.SingleAsync(item => item.Id == member.Id);
        Assert.Equal(ExperienceLevel.Intermediate, stored.ExperienceLevel);
        Assert.Equal([TrainingDay.Monday, TrainingDay.Thursday], stored.GetPlannedTrainingDays());
        Assert.Equal(activeBranchId, stored.PreferredGymBranchId);
    }

    [Fact]
    public async Task InactiveMemberCannotCallRecommendationApiDirectly()
    {
        var member = await CreateUserAsync("inactive-recommendation@test.local", AuthorizationPolicies.Member);
        using var client = CreateClient();
        var token = await LoginAsync(client, member.Email!);
        using var response = await SendJsonAsync(client, HttpMethod.Post, "/api/recommendations/generate", token, new { });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("membership_required", body, StringComparison.Ordinal);
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });

    private async Task<ApplicationUser> CreateUserAsync(string email, string role, bool active = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email, EmailConfirmed = true, FirstName = role, LastName = "Test", IsActive = active };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginPayload>())!.AccessToken;
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string path, string token, object payload)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path, string token)
    {
        using var response = await SendAsync(client, HttpMethod.Get, path, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private sealed record LoginPayload(string AccessToken);
    private sealed record AssignmentPayload(Guid Id);
    private sealed record PlanPayload(Guid Id, string Name, bool IsPublic, string Kind);
    private sealed record AssignedMemberPayload(Guid Id);
}
