using System.Net;
using System.Text;
using FitHub.Application.Common;
using FitHub.Application.Memberships;
using FitHub.Application.Payments;
using FitHub.Domain.Memberships;
using FitHub.Domain.Payments;
using FitHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FitHub.Infrastructure.Memberships;

internal sealed class MembershipService(
    FitHubDbContext dbContext,
    IPaymentGateway paymentGateway,
    TimeProvider timeProvider) : IMembershipService
{
    private const string Currency = "EUR";

    public async Task<MembershipOverviewDto> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentMembershipAsync(userId, cancellationToken);
        if (current is { Status: MembershipStatus.Active } && current.EndsAt <= timeProvider.GetUtcNow())
        {
            current.MarkExpired();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var plans = await GetPlansAsync(false, cancellationToken);
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(payment => payment.UserId == userId)
            .Include(payment => payment.MembershipPlan)
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new PaymentDto(
                payment.Id, payment.Amount, payment.Currency, payment.Status, payment.PurchaseKind,
                payment.MembershipPlan.Name, payment.CreatedAt, payment.PaidAt))
            .ToListAsync(cancellationToken);

        return new MembershipOverviewDto(
            GetEffectiveState(current, payments, timeProvider.GetUtcNow()),
            current is null ? null : MapMembership(current),
            plans,
            payments);
    }

    public async Task<IReadOnlyCollection<MembershipPlanDto>> GetPlansAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await dbContext.MembershipPlans.AsNoTracking()
            .Where(plan => includeInactive || plan.IsActive)
            .OrderBy(plan => plan.Price)
            .Select(plan => new MembershipPlanDto(plan.Id, plan.Name, plan.Description, plan.Price, Currency, plan.BillingPeriodMonths, plan.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<CheckoutSessionDto> CreateCheckoutAsync(Guid userId, CreateCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        MembershipValidators.Validate(command);
        var plan = await dbContext.MembershipPlans.SingleOrDefaultAsync(item => item.Id == command.PlanId && item.IsActive, cancellationToken)
            ?? throw Validation("planId", "The selected membership plan is unavailable.");
        var customerEmail = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);
        var current = await GetCurrentMembershipAsync(userId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var isActive = current?.IsActiveAt(now) == true;

        Membership membership;
        switch (command.PurchaseKind)
        {
            case MembershipPurchaseKind.Purchase when isActive:
                throw Validation("purchaseKind", "Use renewal or upgrade for an active membership.");
            case MembershipPurchaseKind.Renewal when !isActive || current!.MembershipPlanId != plan.Id:
                throw Validation("planId", "Renewal must use the current active plan.");
            case MembershipPurchaseKind.Upgrade when !isActive || current!.MembershipPlanId == plan.Id || plan.Price <= current.MembershipPlan.Price:
                throw Validation("planId", "Upgrade requires a higher-priced active plan.");
            case MembershipPurchaseKind.Purchase:
                membership = new Membership(userId, plan.Id, now, now.AddMonths(plan.BillingPeriodMonths));
                dbContext.Memberships.Add(membership);
                break;
            default:
                membership = current!;
                break;
        }

        var payment = new Payment
        {
            UserId = userId,
            Membership = membership,
            MembershipPlanId = plan.Id,
            PurchaseKind = command.PurchaseKind,
            Amount = plan.Price,
            Currency = Currency,
            Status = PaymentStatus.Pending
        };
        dbContext.Payments.Add(payment);

        var checkout = await paymentGateway.CreateCheckoutAsync(
            new PaymentCheckoutRequest(
                payment.Id, userId, plan.Id, command.PurchaseKind, customerEmail,
                plan.Name, payment.Amount, payment.Currency), cancellationToken);
        payment.ProviderReference = checkout.SessionId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CheckoutSessionDto(payment.Id, checkout.CheckoutUrl);
    }

    public async Task<string?> GetReceiptHtmlAsync(Guid userId, Guid paymentId, bool administrator, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.Payments.AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.MembershipPlan)
            .SingleOrDefaultAsync(item => item.Id == paymentId && (administrator || item.UserId == userId), cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Succeeded) return null;

        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        return $$"""
            <!doctype html><html lang="en"><head><meta charset="utf-8"><title>FitHub receipt {{payment.Id}}</title>
            <style>body{font:16px system-ui;max-width:720px;margin:3rem auto;color:#172033}h1{color:#047857}table{width:100%;border-collapse:collapse}td{padding:.7rem;border-bottom:1px solid #ddd}td:last-child{text-align:right}@media print{button{display:none} }</style></head>
            <body><button onclick="window.print()">Print receipt</button><h1>FitHub payment receipt</h1>
            <p>Receipt <strong>{{payment.Id}}</strong></p><table>
            <tr><td>Member</td><td>{{E(payment.User.FirstName)}} {{E(payment.User.LastName)}}</td></tr>
            <tr><td>Plan</td><td>{{E(payment.MembershipPlan.Name)}}</td></tr>
            <tr><td>Payment type</td><td>{{payment.PurchaseKind}}</td></tr>
            <tr><td>Paid</td><td>{{payment.PaidAt:yyyy-MM-dd HH:mm 'UTC'}}</td></tr>
            <tr><td>Total</td><td><strong>{{payment.Amount:0.00}} {{E(payment.Currency)}}</strong></td></tr>
            </table><p>Payment reference: {{E(payment.ProviderReference)}}</p></body></html>
            """;
    }

    public async Task<MembershipPlanDto> CreatePlanAsync(UpsertMembershipPlanCommand command, CancellationToken cancellationToken = default)
    {
        MembershipValidators.Validate(command);
        var plan = new MembershipPlan();
        plan.Update(command.Name, command.Description, command.Price, command.BillingPeriodMonths);
        dbContext.MembershipPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPlan(plan);
    }

    public async Task<MembershipPlanDto?> UpdatePlanAsync(Guid planId, UpsertMembershipPlanCommand command, CancellationToken cancellationToken = default)
    {
        MembershipValidators.Validate(command);
        var plan = await dbContext.MembershipPlans.FindAsync([planId], cancellationToken);
        if (plan is null) return null;
        plan.Update(command.Name, command.Description, command.Price, command.BillingPeriodMonths);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPlan(plan);
    }

    public async Task<bool> SetPlanActiveAsync(Guid planId, bool active, CancellationToken cancellationToken = default)
    {
        var plan = await dbContext.MembershipPlans.FindAsync([planId], cancellationToken);
        if (plan is null) return false;
        if (active) plan.Activate(); else plan.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> HasActiveMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        return dbContext.Memberships.AnyAsync(item => item.UserId == userId && item.Status == MembershipStatus.Active && item.StartsAt <= now && item.EndsAt > now, cancellationToken);
    }

    private Task<Membership?> GetCurrentMembershipAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Memberships.Include(item => item.MembershipPlan)
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.EndsAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static MembershipPlanDto MapPlan(MembershipPlan plan) =>
        new(plan.Id, plan.Name, plan.Description, plan.Price, Currency, plan.BillingPeriodMonths, plan.IsActive);

    private MembershipDto MapMembership(Membership membership) =>
        new(membership.Id, MapPlan(membership.MembershipPlan), membership.Status, membership.StartsAt, membership.EndsAt, membership.IsActiveAt(timeProvider.GetUtcNow()));

    private static EffectiveMembershipState GetEffectiveState(
        Membership? membership,
        IReadOnlyCollection<PaymentDto> payments,
        DateTimeOffset now)
    {
        if (membership is null) return EffectiveMembershipState.None;
        if (membership.Status == MembershipStatus.Active && membership.StartsAt <= now && membership.EndsAt > now)
            return EffectiveMembershipState.Active;
        if (membership.Status == MembershipStatus.Active && membership.EndsAt <= now)
            return EffectiveMembershipState.Expired;

        return membership.Status switch
        {
            MembershipStatus.Pending when payments.Any(payment =>
                payment.Status == PaymentStatus.Pending && payment.Id != Guid.Empty) => EffectiveMembershipState.PendingPayment,
            MembershipStatus.Expired => EffectiveMembershipState.Expired,
            MembershipStatus.Cancelled => EffectiveMembershipState.Cancelled,
            MembershipStatus.Paused => EffectiveMembershipState.PastDue,
            _ => EffectiveMembershipState.None
        };
    }

    private static CommandValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
