using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FitHub.Application.Authentication;
using FitHub.Application.Payments;
using FitHub.Domain.Identity;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace FitHub.IntegrationTests;

public sealed class AuthenticationApiTests : IAsyncLifetime
{
    private const string ValidPassword = "Test-only-Password-42!";
    private const string SigningKey = "FitHub-tests-only-signing-key-at-least-32-characters-long";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_auth_tests")
        .WithUsername("fithub_auth_tests")
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
            builder.UseSetting("Jwt:SigningKey", SigningKey);
            builder.UseSetting("EquipmentDevices:CardUidPepper", "test-only-card-uid-pepper");
            builder.UseSetting("SeedData:Enabled", "false");
            builder.UseSetting("SeedData:DemoPassword", ValidPassword);
            builder.UseSetting("SeedData:Accounts:0:Email", "no-membership@fithub.demo");
            builder.UseSetting("SeedData:Accounts:0:FirstName", "No Membership");
            builder.UseSetting("SeedData:Accounts:0:LastName", "Member");
            builder.UseSetting("SeedData:Accounts:0:Role", AuthorizationPolicies.Member);
            builder.UseSetting("SeedData:Accounts:0:MembershipState", "None");
            builder.UseSetting("SeedData:Accounts:1:Email", "active-member@fithub.demo");
            builder.UseSetting("SeedData:Accounts:1:FirstName", "Active");
            builder.UseSetting("SeedData:Accounts:1:LastName", "Member");
            builder.UseSetting("SeedData:Accounts:1:Role", AuthorizationPolicies.Member);
            builder.UseSetting("SeedData:Accounts:1:MembershipState", "Active");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton<IPaymentGateway, CheckoutTestGateway>();
                services.RemoveAll<IPasswordResetEmailSender>();
                services.AddSingleton<IPasswordResetEmailSender, TestPasswordResetEmailSender>();
            });
        });

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        await dbContext.Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { AuthorizationPolicies.Member, AuthorizationPolicies.Trainer, AuthorizationPolicies.Administrator })
        {
            Assert.True((await roleManager.CreateAsync(new IdentityRole<Guid>(role))).Succeeded);
        }

        await CreateUserAsync(scope.ServiceProvider, "member.auth@fithub.test", AuthorizationPolicies.Member);
        await CreateUserAsync(scope.ServiceProvider, "no-membership.auth@fithub.test", AuthorizationPolicies.Member);
        await CreateUserAsync(scope.ServiceProvider, "expired.auth@fithub.test", AuthorizationPolicies.Member);
        await CreateUserAsync(scope.ServiceProvider, "trainer.auth@fithub.test", AuthorizationPolicies.Trainer);
        await CreateUserAsync(scope.ServiceProvider, "admin.auth@fithub.test", AuthorizationPolicies.Administrator);

        var member = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByEmailAsync("member.auth@fithub.test");
        var plan = new MembershipPlan { Name = "Monthly", Price = 29.90m, BillingPeriodMonths = 1 };
        var membership = new Membership(member!.Id, plan.Id, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        membership.Activate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var expiredUser = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("expired.auth@fithub.test");
        var expiredMembership = new Membership(expiredUser!.Id, plan.Id, DateTimeOffset.UtcNow.AddMonths(-2), DateTimeOffset.UtcNow.AddMonths(-1));
        expiredMembership.Activate(DateTimeOffset.UtcNow.AddMonths(-2), DateTimeOffset.UtcNow.AddMonths(-1));
        dbContext.AddRange(plan, membership, expiredMembership);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task RegistrationSucceedsAndDuplicateRegistrationFailsSafely()
    {
        using var client = CreateClient();
        var request = new { email = "new.member@fithub.test", firstName = "New", lastName = "Member", password = ValidPassword, consentAccepted = true };

        using var successfulResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        using var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateBody = await duplicateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.DoesNotContain("exists", duplicateBody, StringComparison.OrdinalIgnoreCase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>()
            .FindByEmailAsync(request.email);
        Assert.NotNull(stored!.ConsentAcceptedAtUtc);
        Assert.Equal(RegistrationConsent.CurrentVersion, stored.ConsentVersion);
        var preferenceCount = await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().NotificationPreferences
            .CountAsync(item => item.UserId == stored.Id);
        Assert.Equal(Enum.GetValues<FitHub.Domain.Communications.NotificationType>().Length, preferenceCount);
    }

    [Fact]
    public async Task RegistrationRequiresExplicitConsent()
    {
        using var client = CreateClient();

        using var missing = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "missing-consent@fithub.test",
            firstName = "Missing",
            lastName = "Consent",
            password = ValidPassword
        });
        using var rejected = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "false-consent@fithub.test",
            firstName = "False",
            lastName = "Consent",
            password = ValidPassword,
            consentAccepted = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("ConsentAccepted", await missing.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConsentAccepted", await rejected.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginSucceedsWithCorrectPasswordAndFailsWithSafeMessage()
    {
        using var client = CreateClient();

        using var successfulResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "member.auth@fithub.test", password = ValidPassword });
        using var failedResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "missing@fithub.test", password = "Wrong-password-42!" });
        var failedBody = await failedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, failedResponse.StatusCode);
        Assert.DoesNotContain("account", failedBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndLogoutRevokesReplacement()
    {
        using var client = CreateClient();
        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "member.auth@fithub.test", password = ValidPassword });
        var originalCookie = GetRefreshCookie(loginResponse);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", originalCookie);
        using var refreshResponse = await client.SendAsync(refreshRequest);
        var replacementCookie = GetRefreshCookie(refreshResponse);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", replacementCookie);
        using var logoutResponse = await client.SendAsync(logoutRequest);

        using var revokedRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        revokedRefreshRequest.Headers.Add("Cookie", replacementCookie);
        using var revokedResponse = await client.SendAsync(revokedRefreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotEqual(originalCookie, replacementCookie);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Fact]
    public async Task MemberIsForbiddenAndAdministratorCanAccessAdministratorEndpoint()
    {
        using var client = CreateClient();
        var memberToken = await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test");
        var administratorToken = await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test");

        using var memberRequest = CreateAdministratorRequest(memberToken);
        using var memberResponse = await client.SendAsync(memberRequest);
        using var administratorRequest = CreateAdministratorRequest(administratorToken);
        using var administratorResponse = await client.SendAsync(administratorRequest);

        Assert.Equal(HttpStatusCode.Forbidden, memberResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, administratorResponse.StatusCode);
    }

    [Fact]
    public async Task InvalidRegistrationIsRejectedByApiValidation()
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            firstName = "",
            lastName = "Member",
            password = "weak",
            consentAccepted = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasswordRecoveryDoesNotRevealWhetherAccountExists()
    {
        using var client = CreateClient();
        var sender = _factory.Services.GetRequiredService<IPasswordResetEmailSender>() as TestPasswordResetEmailSender;
        sender!.Clear();
        using var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "member.auth@fithub.test" });
        using var missing = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "missing.auth@fithub.test" });

        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
        Assert.Single(sender.Messages);
    }

    [Fact]
    public async Task MemberCanManageOnlyCurrentNotificationPreferencesAndInvalidCategoryIsRejected()
    {
        using var client = CreateClient();
        var memberToken = await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        using var defaultsResponse = await client.GetAsync("/api/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, defaultsResponse.StatusCode);
        var defaults = await defaultsResponse.Content.ReadFromJsonAsync<PreferencePayload[]>();
        Assert.Equal(Enum.GetValues<FitHub.Domain.Communications.NotificationType>().Length, defaults!.Length);
        Assert.All(defaults, item => Assert.True(item.Enabled));

        using var updateResponse = await client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            preferences = new[] { new { category = "PersonalRecord", enabled = false } }
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PreferencePayload[]>();
        Assert.False(Assert.Single(updated!, item => item.Category == "PersonalRecord").Enabled);

        using var invalid = await client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            preferences = new[] { new { category = "NotARealCategory", enabled = false } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var trainerToken = await LoginAndGetAccessTokenAsync(client, "trainer.auth@fithub.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", trainerToken);
        using var trainer = await client.GetAsync("/api/notifications/preferences");
        Assert.Equal(HttpStatusCode.Forbidden, trainer.StatusCode);
    }

    [Fact]
    public async Task PasswordResetChangesPasswordStoresOnlyHashAndIsSingleUse()
    {
        const string email = "reset-success@fithub.test";
        const string replacement = "Replacement-Password-84!";
        await using (var scope = _factory.Services.CreateAsyncScope())
            await CreateUserAsync(scope.ServiceProvider, email, AuthorizationPolicies.Member);
        var sender = Assert.IsType<TestPasswordResetEmailSender>(_factory.Services.GetRequiredService<IPasswordResetEmailSender>());
        sender.Clear();
        using var client = CreateClient();

        using var request = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var message = Assert.Single(sender.Messages);
        var link = ReadResetLink(message.ResetUrl);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
            var stored = Assert.Single(await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().PasswordResetTokens
                .Where(item => item.UserId == user!.Id).ToListAsync());
            Assert.NotEqual(link.Token, stored.TokenHash);
            Assert.Equal(64, stored.TokenHash.Length);
        }

        using var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = link.Email,
            token = link.Token,
            newPassword = replacement,
            confirmPassword = replacement
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        using var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        using var newLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = replacement });
        using var reused = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = link.Token,
            newPassword = "Another-Replacement-85!",
            confirmPassword = "Another-Replacement-85!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Contains("invalid", await reused.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ModifiedOrMismatchedPasswordResetTokenIsRejected()
    {
        const string email = "reset-mismatch@fithub.test";
        await using (var scope = _factory.Services.CreateAsyncScope())
            await CreateUserAsync(scope.ServiceProvider, email, AuthorizationPolicies.Member);
        var sender = Assert.IsType<TestPasswordResetEmailSender>(_factory.Services.GetRequiredService<IPasswordResetEmailSender>());
        sender.Clear();
        using var client = CreateClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var link = ReadResetLink(Assert.Single(sender.Messages).ResetUrl);

        using var modified = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = link.Token[..^1] + (link.Token[^1] == 'A' ? "B" : "A"),
            newPassword = "Replacement-Password-84!",
            confirmPassword = "Replacement-Password-84!"
        });
        using var mismatched = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "member.auth@fithub.test",
            token = link.Token,
            newPassword = "Replacement-Password-84!",
            confirmPassword = "Replacement-Password-84!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, modified.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, mismatched.StatusCode);
    }

    [Fact]
    public async Task NewPasswordResetRequestInvalidatesPreviousOutstandingToken()
    {
        const string email = "reset-superseded@fithub.test";
        await using (var scope = _factory.Services.CreateAsyncScope())
            await CreateUserAsync(scope.ServiceProvider, email, AuthorizationPolicies.Member);
        var sender = Assert.IsType<TestPasswordResetEmailSender>(_factory.Services.GetRequiredService<IPasswordResetEmailSender>());
        sender.Clear();
        using var client = CreateClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var first = ReadResetLink(Assert.Single(sender.Messages).ResetUrl);
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var second = ReadResetLink(sender.Messages.Last().ResetUrl);

        using var firstAttempt = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = first.Token,
            newPassword = "Replacement-Password-84!",
            confirmPassword = "Replacement-Password-84!"
        });
        using var secondAttempt = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = second.Token,
            newPassword = "Replacement-Password-84!",
            confirmPassword = "Replacement-Password-84!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, firstAttempt.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task ExpiredPasswordResetTokenAndWeakPasswordAreRejected()
    {
        const string email = "reset-expired@fithub.test";
        await using (var scope = _factory.Services.CreateAsyncScope())
            await CreateUserAsync(scope.ServiceProvider, email, AuthorizationPolicies.Member);
        var sender = Assert.IsType<TestPasswordResetEmailSender>(_factory.Services.GetRequiredService<IPasswordResetEmailSender>());
        sender.Clear();
        using var client = CreateClient();
        await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var first = ReadResetLink(Assert.Single(sender.Messages).ResetUrl);

        using (var weak = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = first.Token,
            newPassword = "weak-password",
            confirmPassword = "weak-password"
        }))
            Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE \"PasswordResetTokens\" SET \"ExpiresAtUtc\" = {0} WHERE \"TokenHash\" = {1}",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(first.Token))));
        }

        using var expired = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = first.Token,
            newPassword = "Replacement-Password-84!",
            confirmPassword = "Replacement-Password-84!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
    }

    [Fact]
    public async Task ParkingSignalRNegotiationAcceptsJwtQueryToken()
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test");
        using var response = await client.PostAsync(
            $"/hubs/parking/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(token)}",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FaultPhotographsRequireAuthentication()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/fault-images/not-present.png");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("no-membership.auth@fithub.test")]
    [InlineData("expired.auth@fithub.test")]
    public async Task InactiveMemberReceivesSafeMembershipRequiredProblem(string email)
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, email);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("membership_required", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ActiveMembershipHandler", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActiveMemberCanAccessPersonalMemberEndpoint()
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("trainer.auth@fithub.test")]
    [InlineData("admin.auth@fithub.test")]
    public async Task StaffCannotAccessPersonalMemberEndpoint(string email)
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, email);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("trainer.auth@fithub.test")]
    [InlineData("admin.auth@fithub.test")]
    public async Task StaffCannotAccessPersonalMembershipOverview(string email)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, email));
        using var response = await client.GetAsync("/api/membership");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/equipment")]
    [InlineData("/api/parking")]
    public async Task FacilityOperationsAllowAdministratorButNotTrainer(string path)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "trainer.auth@fithub.test"));
        using var trainer = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, trainer.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test"));
        using var administrator = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, administrator.StatusCode);
    }

    [Fact]
    public async Task NewMemberCompletesOnboardingOnceThroughProfileApi()
    {
        using var client = CreateClient();
        var email = $"onboarding-{Guid.NewGuid():N}@fithub.test";
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            firstName = "New",
            lastName = "Member",
            password = ValidPassword,
            consentAccepted = true
        });
        registration.EnsureSuccessStatusCode();
        var auth = await registration.Content.ReadFromJsonAsync<AuthenticationPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        using var before = await client.GetAsync("/api/auth/me");
        var beforeBody = await before.Content.ReadAsStringAsync();
        Assert.Contains("\"onboardingCompleted\":false", beforeBody, StringComparison.OrdinalIgnoreCase);

        Guid branchId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var branch = new GymBranch("ONBOARD", "Onboarding Test Branch");
            scope.ServiceProvider.GetRequiredService<FitHubDbContext>().GymBranches.Add(branch);
            await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().SaveChangesAsync();
            branchId = branch.Id;
        }

        using var update = await client.PutAsJsonAsync("/api/profile", new
        {
            preferredName = "Newbie",
            dateOfBirth = new DateOnly(1998, 5, 20),
            heightCm = 175.5m,
            weightKg = 72.4m,
            fitnessGoal = "Strength",
            activityLevel = "Moderate",
            experienceLevel = "Beginner",
            plannedTrainingDays = new[] { "Monday", "Wednesday" },
            preferredGymBranchId = branchId
        });
        update.EnsureSuccessStatusCode();

        using var after = await client.GetAsync("/api/auth/me");
        var afterBody = await after.Content.ReadAsStringAsync();
        Assert.Contains("\"onboardingCompleted\":true", afterBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TrainerClassRequiresApprovalBeforeMemberCanSeeIt()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "trainer.auth@fithub.test"));
        var start = DateTimeOffset.UtcNow.AddDays(3);
        using var create = await client.PostAsJsonAsync("/api/trainer/classes", new
        {
            name = "Approval integration class",
            description = "Pending until an administrator reviews it.",
            startsAt = start,
            endsAt = start.AddHours(1),
            capacity = 12,
            location = "Studio 2"
        });
        create.EnsureSuccessStatusCode();
        var schedule = await create.Content.ReadFromJsonAsync<ClassSchedulePayload>();
        Assert.Equal("PendingApproval", schedule!.Status);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test"));
        var beforeApproval = await client.GetFromJsonAsync<ClassSchedulePayload[]>("/api/classes");
        Assert.DoesNotContain(beforeApproval!, item => item.Id == schedule.Id);
        using var memberCreateAttempt = await client.PostAsJsonAsync("/api/trainer/classes", new
        {
            name = "Forbidden", startsAt = start, endsAt = start.AddHours(1), capacity = 5
        });
        Assert.Equal(HttpStatusCode.Forbidden, memberCreateAttempt.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test"));
        using var approve = await client.PutAsJsonAsync($"/api/administration/classes/{schedule.Id}/review", new
        {
            approve = true,
            reason = (string?)null
        });
        approve.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "member.auth@fithub.test"));
        var afterApproval = await client.GetFromJsonAsync<ClassSchedulePayload[]>("/api/classes");
        Assert.Contains(afterApproval!, item => item.Id == schedule.Id && item.Status == "Approved");
    }

    [Fact]
    public async Task OnlyAdministratorCanCreateTrainerAccount()
    {
        using var client = CreateClient();
        var email = $"trainer-{Guid.NewGuid():N}@fithub.test";
        var request = new { email, firstName = "Created", lastName = "Trainer", temporaryPassword = ValidPassword };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "trainer.auth@fithub.test"));
        using var trainerAttempt = await client.PostAsJsonAsync("/api/administration/trainers", request);
        Assert.Equal(HttpStatusCode.Forbidden, trainerAttempt.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test"));
        using var adminAttempt = await client.PostAsJsonAsync("/api/administration/trainers", request);
        Assert.Equal(HttpStatusCode.Created, adminAttempt.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        _ = await LoginAndGetAccessTokenAsync(client, email);
    }

    [Fact]
    public async Task BlockingUserInvalidatesExistingAccessToken()
    {
        var email = $"blocked-{Guid.NewGuid():N}@fithub.test";
        Guid userId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await CreateUserAsync(scope.ServiceProvider, email, AuthorizationPolicies.Member);
            userId = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id;
        }

        using var client = CreateClient();
        var memberToken = await LoginAndGetAccessTokenAsync(client, email);
        var adminToken = await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var blocked = await client.PutAsJsonAsync($"/api/administration/users/{userId}/active", new { active = false });
        Assert.Equal(HttpStatusCode.NoContent, blocked.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        using var currentUser = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, currentUser.StatusCode);
    }

    [Fact]
    public async Task AdministratorCanValidateButCannotBypassInvalidCredential()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetAccessTokenAsync(client, "admin.auth@fithub.test"));
        using var response = await client.PostAsJsonAsync("/api/attendance/validate-access", new { token = "not-a-real-card" });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccessValidationPayload>();
        Assert.Equal("INVALID_TOKEN", result!.Code);
    }

    [Fact]
    public async Task InactiveMemberCanListPlansAndCheckoutWithStringPurchaseKindAtServerPrice()
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, "no-membership.auth@fithub.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var plans = await client.GetFromJsonAsync<PlanPayload[]>("/api/membership/plans");
        var plan = Assert.Single(plans!);

        using var response = await client.PostAsJsonAsync("/api/membership/checkout", new
        {
            planId = plan.Id,
            purchaseKind = "Purchase",
            price = 0.01m
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutPayload>();
        Assert.StartsWith("https://checkout.stripe.test/", checkout!.CheckoutUrl, StringComparison.Ordinal);

        await using var scope = _factory.Services.CreateAsyncScope();
        var user = Assert.IsType<ApplicationUser>(await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("no-membership.auth@fithub.test"));
        var payment = await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Payments
            .OrderByDescending(item => item.CreatedAt).FirstAsync(item => item.UserId == user.Id);
        Assert.Equal(plan.Price, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(MembershipStatus.Pending, (await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().Memberships.FindAsync(payment.MembershipId))!.Status);
        var gatewayRequest = Assert.IsType<CheckoutTestGateway>(scope.ServiceProvider.GetRequiredService<IPaymentGateway>()).LastRequest;
        Assert.NotNull(gatewayRequest);
        Assert.Equal(user.Id, gatewayRequest.UserId);
        Assert.Equal(plan.Id, gatewayRequest.PlanId);
        Assert.Equal(MembershipPurchaseKind.Purchase, gatewayRequest.PurchaseKind);
        Assert.Equal("no-membership.auth@fithub.test", gatewayRequest.CustomerEmail);
        Assert.Equal("EUR", gatewayRequest.Currency);
        Assert.Equal(29.90m, gatewayRequest.Amount);
        Assert.Equal(2990, PaymentAmount.ToMinorUnits(gatewayRequest.Amount));
    }

    [Fact]
    public async Task CheckoutRequiresAuthentication()
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/membership/checkout", new { planId = Guid.NewGuid(), purchaseKind = "Purchase" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InactivePlanIsRejectedWithValidationProblem()
    {
        Guid planId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var plan = new MembershipPlan { Name = $"Inactive-{Guid.NewGuid():N}", Price = 25m, BillingPeriodMonths = 1 };
            plan.Deactivate();
            scope.ServiceProvider.GetRequiredService<FitHubDbContext>().MembershipPlans.Add(plan);
            await scope.ServiceProvider.GetRequiredService<FitHubDbContext>().SaveChangesAsync();
            planId = plan.Id;
        }
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAndGetAccessTokenAsync(client, "no-membership.auth@fithub.test"));
        using var response = await client.PostAsJsonAsync("/api/membership/checkout", new { planId, purchaseKind = "Purchase" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("unavailable", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckoutInitializationFailureDoesNotCreatePaymentHistoryEntry()
    {
        await using var beforeScope = _factory.Services.CreateAsyncScope();
        var user = await beforeScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("no-membership.auth@fithub.test");
        var before = await beforeScope.ServiceProvider.GetRequiredService<FitHubDbContext>().Payments.CountAsync(item => item.UserId == user!.Id);

        await using var failureFactory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FailingCheckoutGateway>();
        }));
        using var client = failureFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAndGetAccessTokenAsync(client, "no-membership.auth@fithub.test"));
        var plan = Assert.Single((await client.GetFromJsonAsync<PlanPayload[]>("/api/membership/plans"))!);
        using var response = await client.PostAsJsonAsync("/api/membership/checkout", new { planId = plan.Id, purchaseKind = "Purchase" });
        var body = await response.Content.ReadAsStringAsync();

        await using var afterScope = _factory.Services.CreateAsyncScope();
        var after = await afterScope.ServiceProvider.GetRequiredService<FitHubDbContext>().Payments.CountAsync(item => item.UserId == user!.Id);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(before, after);
        Assert.Contains("CHECKOUT_CREATION_FAILED", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Simulated", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidPurchaseKindReturnsSafeValidationProblem()
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, "no-membership.auth@fithub.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var plan = Assert.Single((await client.GetFromJsonAsync<PlanPayload[]>("/api/membership/plans"))!);

        using var response = await client.PostAsJsonAsync("/api/membership/checkout", new { planId = plan.Id, purchaseKind = "Anything" });
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("validation_failed", body, StringComparison.Ordinal);
        Assert.DoesNotContain("could not be converted", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CreateCheckoutRequest", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DevelopmentDemoSeedingIsIdempotent()
    {
        await using var firstScope = _factory.Services.CreateAsyncScope();
        await firstScope.ServiceProvider.GetRequiredService<FitHub.Infrastructure.Persistence.Seeding.DevelopmentDataSeeder>().SeedAsync();
        var firstUsers = firstScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var firstActive = await firstUsers.FindByEmailAsync("active-member@fithub.demo");
        await firstScope.ServiceProvider.GetRequiredService<FitHub.Application.Notifications.INotificationService>()
            .UpdatePreferencesAsync(firstActive!.Id, [new("PersonalRecord", false)]);
        await using var secondScope = _factory.Services.CreateAsyncScope();
        await secondScope.ServiceProvider.GetRequiredService<FitHub.Infrastructure.Persistence.Seeding.DevelopmentDataSeeder>().SeedAsync();

        var db = secondScope.ServiceProvider.GetRequiredService<FitHubDbContext>();
        var users = secondScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var inactive = await users.FindByEmailAsync("no-membership@fithub.demo");
        var active = await users.FindByEmailAsync("active-member@fithub.demo");
        Assert.NotNull(inactive); Assert.NotNull(active);
        Assert.Equal(0, await db.Memberships.CountAsync(item => item.UserId == inactive!.Id));
        Assert.Equal(1, await db.Memberships.CountAsync(item => item.UserId == active!.Id));
        Assert.Equal(1, await db.Payments.CountAsync(item => item.UserId == active!.Id && item.Status == PaymentStatus.Succeeded));
        var monthly = await db.MembershipPlans.SingleAsync(item => item.Name == "Monthly");
        Assert.Equal(29.90m, monthly.Price);
        Assert.Equal(1, monthly.BillingPeriodMonths);
        var payment = await db.Payments.SingleAsync(item => item.UserId == active.Id && item.Status == PaymentStatus.Succeeded);
        Assert.Equal(29.90m, payment.Amount);
        var savedPreference = await db.NotificationPreferences.SingleAsync(item =>
            item.UserId == active.Id && item.Category == FitHub.Domain.Communications.NotificationType.PersonalRecord);
        Assert.False(savedPreference.Enabled);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false,
        AllowAutoRedirect = false
    });

    private static async Task CreateUserAsync(IServiceProvider services, string email, string role)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            FirstName = role,
            LastName = "Test"
        };
        Assert.True((await userManager.CreateAsync(user, ValidPassword)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
    }

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var values));
        return Assert.Single(values, value => value.StartsWith("fithub.refresh=", StringComparison.Ordinal)).Split(';')[0];
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ValidPassword });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthenticationPayload>();
        return Assert.IsType<string>(payload?.AccessToken);
    }

    private static HttpRequestMessage CreateAdministratorRequest(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/authorization/administrator");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static (string Email, string Token) ReadResetLink(string resetUrl)
    {
        var query = new Uri(resetUrl).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(part => part[0], part => Uri.UnescapeDataString(part[1]), StringComparer.OrdinalIgnoreCase);
        return (query["email"], query["token"]);
    }

    private sealed record AuthenticationPayload(string AccessToken);
    private sealed record PlanPayload(Guid Id, decimal Price);
    private sealed record CheckoutPayload(string CheckoutUrl);
    private sealed record ClassSchedulePayload(Guid Id, string Status);
    private sealed record AccessValidationPayload(string Code);
    private sealed record PreferencePayload(string Category, bool Enabled);

    private sealed class CheckoutTestGateway : IPaymentGateway
    {
        public PaymentCheckoutRequest? LastRequest { get; private set; }
        public Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default) =>
            Capture(request);
        private Task<PaymentCheckoutResult> Capture(PaymentCheckoutRequest request)
        {
            LastRequest = request;
            return Task.FromResult(new PaymentCheckoutResult($"cs_test_{request.PaymentId:N}", "https://checkout.stripe.test/session"));
        }
        public PaymentWebhookNotification ParseWebhook(string payload, string signature) =>
            throw new PaymentWebhookValidationException("Test gateway does not process webhooks.");
    }

    private sealed class FailingCheckoutGateway : IPaymentGateway
    {
        public Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated Stripe initialization failure with internal detail.");
        public PaymentWebhookNotification ParseWebhook(string payload, string signature) => throw new NotSupportedException();
    }

    private sealed class TestPasswordResetEmailSender : IPasswordResetEmailSender
    {
        private readonly List<ResetEmail> _messages = [];
        public IReadOnlyList<ResetEmail> Messages => _messages;
        public void Clear() => _messages.Clear();
        public Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, TimeSpan validity, CancellationToken cancellationToken = default)
        {
            _messages.Add(new ResetEmail(recipientEmail, resetUrl));
            return Task.CompletedTask;
        }
    }

    private sealed record ResetEmail(string RecipientEmail, string ResetUrl);
}
