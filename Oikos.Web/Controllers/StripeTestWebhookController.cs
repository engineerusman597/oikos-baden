using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.Stripe;
using Stripe;

namespace Oikos.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/stripe/webhook_test")]
public class StripeTestWebhookController : ControllerBase
{
    private readonly StripeOptions _options;
    private readonly IStripeWebhookService _webhookService;
    private readonly ILogger<StripeTestWebhookController> _logger;

    public StripeTestWebhookController(
        IOptionsSnapshot<StripeOptions> optionsSnapshot,
        IStripeWebhookService webhookService,
        ILogger<StripeTestWebhookController> logger)
    {
        _options = optionsSnapshot.Get("StripeTest");
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleAsync(CancellationToken cancellationToken)
    {
        string payload;

        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        var signatureHeader = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                _options.WebhookSecret,
                tolerance: 300,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe signature for test webhook");
            return BadRequest();
        }

        try
        {
            await _webhookService.ProcessEventAsync(stripeEvent, payload, _options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing Stripe test webhook event {EventId}", stripeEvent.Id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }
}
