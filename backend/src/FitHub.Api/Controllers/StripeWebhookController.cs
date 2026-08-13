using FitHub.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitHub.Api.Controllers;

[ApiController]
[Route("api/payments/stripe/webhook")]
public sealed class StripeWebhookController(
    IPaymentWebhookService webhookService,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        try
        {
            var result = await webhookService.ProcessAsync(payload, signature, cancellationToken);
            return Ok(result);
        }
        catch (PaymentWebhookValidationException exception)
        {
            logger.LogWarning("A Stripe webhook was rejected ({ReasonType}).", exception.GetType().Name);
            return BadRequest(new { message = "Webhook signature or payload is invalid." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Stripe webhook processing failed. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "WEBHOOK_PROCESSING_FAILED",
                message = "The webhook could not be processed.",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }
}
