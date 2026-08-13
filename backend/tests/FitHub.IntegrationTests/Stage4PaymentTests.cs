using System.Net;
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

public sealed class Stage4PaymentTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fithub_stage4_tests")
        .WithUsername("fithub_stage4_tests")
        .WithPassword("Test-only-database-password-42!")
        .Build();
    private WebApplicationFactory<Program> _factory = null!;
    private readonly FakePaymentGateway _gateway = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:FitHubDatabase", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "FitHub-tests-only-signing-key-at-least-32-characters-long");
            builder.UseSetting("SeedData:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton<IPaymentGateway>(_gateway);
            });
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
    public async Task DuplicateWebhookActivatesMembershipOnlyOnce()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<FitHubDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "stage4@fithub.test", Email = "stage4@fithub.test", FirstName = "Stage", LastName = "Four" };
        Assert.True((await userManager.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update("Monthly", "Test plan", 49.90m, 1);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(user.Id, plan.Id, now, now.AddMonths(1));
        var payment = new Payment
        {
            UserId = user.Id,
            Membership = membership,
            MembershipPlan = plan,
            PurchaseKind = MembershipPurchaseKind.Purchase,
            Amount = plan.Price,
            Currency = "EUR",
            ProviderReference = FakePaymentGateway.SessionId
        };
        dbContext.AddRange(plan, membership, payment);
        await dbContext.SaveChangesAsync();
        _gateway.Notification = new(
            "evt_stage4_once", "checkout.session.completed", FakePaymentGateway.SessionId,
            payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, PaymentWebhookOutcome.Succeeded);
        var service = services.GetRequiredService<IPaymentWebhookService>();

        var first = await service.ProcessAsync("payload", "signature");
        var second = await service.ProcessAsync("payload", "signature");

        dbContext.ChangeTracker.Clear();
        var storedPayment = await dbContext.Payments.SingleAsync(item => item.Id == payment.Id);
        var storedMembership = await dbContext.Memberships.SingleAsync(item => item.Id == membership.Id);
        Assert.True(first.Processed);
        Assert.True(second.Duplicate);
        Assert.Equal(PaymentStatus.Succeeded, storedPayment.Status);
        Assert.Equal(MembershipStatus.Active, storedMembership.Status);
        Assert.Equal(1, await dbContext.ProcessedPaymentWebhooks.CountAsync(item => item.EventId == "evt_stage4_once"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public async Task PaidCheckoutAppliesExactPlanDuration(int billingPeriodMonths)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<FitHubDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"duration-{billingPeriodMonths}@fithub.test", Email = $"duration-{billingPeriodMonths}@fithub.test", FirstName = "Duration", LastName = "Test" };
        Assert.True((await users.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update($"Duration {billingPeriodMonths}", "Duration test", 50m * billingPeriodMonths, billingPeriodMonths);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(user.Id, plan.Id, now, now.AddMonths(billingPeriodMonths));
        var payment = new Payment { UserId = user.Id, Membership = membership, MembershipPlan = plan, PurchaseKind = MembershipPurchaseKind.Purchase, Amount = plan.Price, Currency = "EUR", ProviderReference = $"cs_test_duration_{billingPeriodMonths}" };
        db.AddRange(plan, membership, payment);
        await db.SaveChangesAsync();
        _gateway.Notification = new(
            $"evt_duration_{billingPeriodMonths}", "checkout.session.completed", payment.ProviderReference,
            payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, PaymentWebhookOutcome.Succeeded);

        await services.GetRequiredService<IPaymentWebhookService>().ProcessAsync("payload", "signature");
        db.ChangeTracker.Clear();
        var stored = await db.Memberships.SingleAsync(item => item.Id == membership.Id);
        Assert.Equal(MembershipStatus.Active, stored.Status);
        Assert.Equal(stored.StartsAt.AddMonths(billingPeriodMonths), stored.EndsAt);
        Assert.Equal(PaymentStatus.Succeeded, (await db.Payments.SingleAsync(item => item.Id == payment.Id)).Status);
    }

    [Theory]
    [InlineData(PaymentWebhookOutcome.Failed, PaymentStatus.Failed)]
    [InlineData(PaymentWebhookOutcome.Cancelled, PaymentStatus.Cancelled)]
    public async Task FailedOrExpiredCheckoutDoesNotActivateMembership(
        PaymentWebhookOutcome outcome,
        PaymentStatus expectedStatus)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<FitHubDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = outcome.ToString().ToLowerInvariant();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"{suffix}@fithub.test", Email = $"{suffix}@fithub.test", FirstName = "Outcome", LastName = "Test" };
        Assert.True((await users.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update($"Outcome {suffix}", "Outcome test", 49.90m, 1);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(user.Id, plan.Id, now, now.AddMonths(1));
        var payment = new Payment { UserId = user.Id, Membership = membership, MembershipPlan = plan, PurchaseKind = MembershipPurchaseKind.Purchase, Amount = plan.Price, Currency = "EUR", ProviderReference = $"cs_test_{suffix}" };
        db.AddRange(plan, membership, payment);
        await db.SaveChangesAsync();
        _gateway.Notification = new(
            $"evt_{suffix}", outcome == PaymentWebhookOutcome.Cancelled ? "checkout.session.expired" : "checkout.session.async_payment_failed",
            payment.ProviderReference, payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, outcome);

        await services.GetRequiredService<IPaymentWebhookService>().ProcessAsync("payload", "signature");
        db.ChangeTracker.Clear();
        Assert.Equal(MembershipStatus.Pending, (await db.Memberships.SingleAsync(item => item.Id == membership.Id)).Status);
        Assert.Equal(expectedStatus, (await db.Payments.SingleAsync(item => item.Id == payment.Id)).Status);
    }

    [Fact]
    public async Task InvalidWebhookSignatureReturnsSafeError()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/stripe/webhook")
        {
            Content = new StringContent("untrusted payload")
        };
        request.Headers.Add("Stripe-Signature", "invalid-signature");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("signature or payload is invalid", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("configuration", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("untrusted payload", body, StringComparison.Ordinal);
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        public const string SessionId = "cs_test_stage4";
        public PaymentWebhookNotification? Notification { get; set; }
        public Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentCheckoutResult(SessionId, "https://checkout.stripe.test/session"));

        public PaymentWebhookNotification ParseWebhook(string payload, string signature)
        {
            if (signature != "signature")
            {
                throw new PaymentWebhookValidationException("Test-only invalid signature detail.");
            }

            return Notification ?? throw new InvalidOperationException("Test webhook notification is not configured.");
        }
    }
}
