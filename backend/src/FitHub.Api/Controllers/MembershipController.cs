using FitHub.Api.Infrastructure;
using FitHub.Api.Models.Memberships;
using FitHub.Application.Authentication;
using FitHub.Application.Common;
using FitHub.Application.Memberships;
using FitHub.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/membership")]
public sealed class MembershipController(
    IMembershipService membershipService,
    IPaymentReconciliationService paymentReconciliationService,
    ILogger<MembershipController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<MembershipOverviewDto> GetOverview(CancellationToken cancellationToken) =>
        membershipService.GetOverviewAsync(User.GetRequiredUserId(), cancellationToken);

    [HttpGet("plans")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<IReadOnlyCollection<MembershipPlanDto>> GetPlans(CancellationToken cancellationToken) =>
        membershipService.GetPlansAsync(false, cancellationToken);

    [HttpPost("checkout")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public async Task<ActionResult<CheckoutSessionDto>> CreateCheckout(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await membershipService.CreateCheckoutAsync(
                User.GetRequiredUserId(), new CreateCheckoutCommand(request.PlanId, request.PurchaseKind), cancellationToken));
        }
        catch (CommandValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Checkout creation failed. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Checkout unavailable",
                Detail = "Checkout could not be started."
            };
            problem.Extensions["code"] = "CHECKOUT_CREATION_FAILED";
            problem.Extensions["message"] = "Checkout could not be started.";
            problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
            return StatusCode(problem.Status.Value, problem);
        }
    }

    [HttpPost("checkout/{sessionId}/reconcile")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<ActionResult<PaymentReconciliationResult>> ReconcileCheckout(
        string sessionId,
        CancellationToken cancellationToken) =>
        ReconcileCheckoutInternal(sessionId, cancellationToken);

    [HttpPost("checkout/reconcile")]
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public Task<ActionResult<PaymentReconciliationResult>> ReconcilePendingCheckout(
        CancellationToken cancellationToken) =>
        ReconcileCheckoutInternal(null, cancellationToken);

    private async Task<ActionResult<PaymentReconciliationResult>> ReconcileCheckoutInternal(
        string? sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await paymentReconciliationService.ReconcileAsync(
                User.GetRequiredUserId(), sessionId, cancellationToken));
        }
        catch (CommandValidationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Checkout reconciliation failed. CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Payment confirmation unavailable",
                Detail = "Payment confirmation could not be completed."
            };
            problem.Extensions["code"] = "PAYMENT_CONFIRMATION_FAILED";
            problem.Extensions["message"] = "Payment confirmation could not be completed.";
            problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
            return StatusCode(problem.Status.Value, problem);
        }
    }

    [HttpGet("payments/{paymentId:guid}/receipt")]
    [Authorize(Roles = AuthorizationPolicies.Member + "," + AuthorizationPolicies.Administrator)]
    public async Task<IActionResult> GetReceipt(Guid paymentId, CancellationToken cancellationToken)
    {
        var html = await membershipService.GetReceiptHtmlAsync(
            User.GetRequiredUserId(), paymentId, User.IsInRole(AuthorizationPolicies.Administrator), cancellationToken);
        return html is null ? NotFound() : Content(html, "text/html; charset=utf-8");
    }
}
