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
        var plan = new MembershipPlan { Name = "Authentication test plan", Price = 20m, BillingPeriodMonths = 1 };
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
        var request = new { email = "new.member@fithub.test", firstName = "New", lastName = "Member", password = ValidPassword };

        using var successfulResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        using var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateBody = await duplicateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.DoesNotContain("exists", duplicateBody, StringComparison.OrdinalIgnoreCase);
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
            password = "weak"
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
        using var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "member.auth@fithub.test" });
        using var missing = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "missing.auth@fithub.test" });

        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
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

    [Theory]
    [InlineData("member.auth@fithub.test")]
    [InlineData("trainer.auth@fithub.test")]
    [InlineData("admin.auth@fithub.test")]
    public async Task ActiveMemberAndStaffCanAccessMembershipProtectedEndpoint(string email)
    {
        using var client = CreateClient();
        var token = await LoginAndGetAccessTokenAsync(client, email);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        Assert.Equal(plan.Price, gatewayRequest.Amount);
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

    private sealed record AuthenticationPayload(string AccessToken);
    private sealed record PlanPayload(Guid Id, decimal Price);
    private sealed record CheckoutPayload(string CheckoutUrl);

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
}
