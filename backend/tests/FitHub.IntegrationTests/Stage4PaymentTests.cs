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
    private readonly FakePaymentCheckoutVerifier _checkoutVerifier = new();

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
                services.RemoveAll<IPaymentCheckoutVerifier>();
                services.AddSingleton<IPaymentCheckoutVerifier>(_checkoutVerifier);
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
            payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, PaymentWebhookOutcome.Succeeded,
            "pi_test_stage4", null, "EUR", 4990, false);
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

    [Fact]
    public async Task PaidCheckoutReconciliationRecoversMissedWebhookIdempotently()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<FitHubDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "reconcile@fithub.test", Email = "reconcile@fithub.test", FirstName = "Reconcile", LastName = "Test" };
        Assert.True((await users.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update("Recovery monthly", "Recovery test", 49.90m, 1);
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
            ProviderReference = "cs_test_recovery"
        };
        db.AddRange(plan, membership, payment);
        await db.SaveChangesAsync();
        _checkoutVerifier.Verification = new(
            payment.ProviderReference, payment.Id, user.Id, plan.Id,
            MembershipPurchaseKind.Purchase, true, true, false, 4990, "eur");
        var service = services.GetRequiredService<IPaymentReconciliationService>();

        var first = await service.ReconcileAsync(user.Id, null);
        var second = await service.ReconcileAsync(user.Id, payment.ProviderReference);

        db.ChangeTracker.Clear();
        var storedPayment = await db.Payments.SingleAsync(item => item.Id == payment.Id);
        var storedMembership = await db.Memberships.SingleAsync(item => item.Id == membership.Id);
        Assert.True(first.Confirmed);
        Assert.True(first.Changed);
        Assert.True(second.Confirmed);
        Assert.False(second.Changed);
        Assert.Equal(PaymentStatus.Succeeded, storedPayment.Status);
        Assert.Equal(MembershipStatus.Active, storedMembership.Status);
        Assert.Equal(storedMembership.StartsAt.AddMonths(1), storedMembership.EndsAt);
        Assert.Equal(1, await db.Memberships.CountAsync(item => item.UserId == user.Id));
    }

    [Fact]
    public async Task UnpaidOrMismatchedCheckoutCannotActivateMembership()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<FitHubDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "unpaid@fithub.test", Email = "unpaid@fithub.test", FirstName = "Unpaid", LastName = "Test" };
        Assert.True((await users.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan();
        plan.Update("Unpaid monthly", "Unpaid test", 49.90m, 1);
        var now = DateTimeOffset.UtcNow;
        var membership = new Membership(user.Id, plan.Id, now, now.AddMonths(1));
        var payment = new Payment { UserId = user.Id, Membership = membership, MembershipPlan = plan, PurchaseKind = MembershipPurchaseKind.Purchase, Amount = plan.Price, Currency = "EUR", ProviderReference = "cs_test_unpaid" };
        db.AddRange(plan, membership, payment);
        await db.SaveChangesAsync();
        var service = services.GetRequiredService<IPaymentReconciliationService>();
        _checkoutVerifier.Verification = new(payment.ProviderReference, payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, false, false, false, 4990, "eur");

        var unpaid = await service.ReconcileAsync(user.Id, payment.ProviderReference);

        Assert.False(unpaid.Confirmed);
        Assert.Equal(MembershipStatus.Pending, membership.Status);
        _checkoutVerifier.Verification = new(payment.ProviderReference, payment.Id, Guid.NewGuid(), plan.Id, MembershipPurchaseKind.Purchase, true, true, false, 4990, "eur");
        await Assert.ThrowsAsync<PaymentCheckoutVerificationException>(() =>
            service.ReconcileAsync(user.Id, payment.ProviderReference));
        Assert.Equal(MembershipStatus.Pending, membership.Status);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task CheckoutReconciliationRequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync(
            "/api/membership/checkout/cs_test_recovery/reconcile",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            payment.Id, user.Id, plan.Id, MembershipPurchaseKind.Purchase, PaymentWebhookOutcome.Succeeded,
            $"pi_test_duration_{billingPeriodMonths}", null, "EUR", checked((long)(payment.Amount * 100m)), false);

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
    public async Task RefundWebhookRecordsCumulativeRefundOnceByPaymentIntent()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<FitHubDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "refund@fithub.test", Email = "refund@fithub.test", FirstName = "Refund", LastName = "Test" };
        Assert.True((await users.CreateAsync(user, "Test-only-Password-42!")).Succeeded);
        var plan = new MembershipPlan { Name = "Refund plan", Price = 49.90m, BillingPeriodMonths = 1 };
        var payment = new Payment
        {
            UserId = user.Id,
            MembershipPlan = plan,
            PurchaseKind = MembershipPurchaseKind.Purchase,
            Amount = 49.90m,
            Currency = "EUR"
        };
        payment.MarkSucceeded(DateTimeOffset.UtcNow, "pi_test_refund_once");
        db.AddRange(plan, payment);
        await db.SaveChangesAsync();
        _gateway.Notification = new(
            "evt_refund_once", "charge.refunded", string.Empty, null, null, null, null,
            PaymentWebhookOutcome.Refunded, "pi_test_refund_once", 4990, "eur");
        var webhook = services.GetRequiredService<IPaymentWebhookService>();

        var first = await webhook.ProcessAsync("payload", "signature");
        var duplicate = await webhook.ProcessAsync("payload", "signature");
        _gateway.Notification = new(
            "evt_refund_late_failure", "checkout.session.async_payment_failed", string.Empty, null, null, null, null,
            PaymentWebhookOutcome.Failed, "pi_test_refund_once");
        await webhook.ProcessAsync("payload", "signature");

        db.ChangeTracker.Clear();
        var stored = await db.Payments.SingleAsync(item => item.Id == payment.Id);
        Assert.True(first.Processed);
        Assert.True(duplicate.Duplicate);
        Assert.Equal(PaymentStatus.Refunded, stored.Status);
        Assert.Equal(49.90m, stored.RefundedAmount);
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

    private sealed class FakePaymentCheckoutVerifier : IPaymentCheckoutVerifier
    {
        public PaymentCheckoutVerification? Verification { get; set; }

        public Task<PaymentCheckoutVerification> VerifyAsync(
            string checkoutSessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Verification ?? throw new InvalidOperationException(
                "Test checkout verification is not configured."));
    }
}
