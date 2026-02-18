using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Oikos.Application.Services.Stripe;
using Stripe;

namespace Oikos.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/stripe-bonix/webhook")]
public class StripeBonixWebhookController : ControllerBase
{
    private readonly StripeOptions _options;
    private readonly IStripeWebhookService _webhookService;
    private readonly ILogger<StripeBonixWebhookController> _logger;

    public StripeBonixWebhookController(
        IOptionsSnapshot<StripeOptions> options,
        IStripeWebhookService webhookService,
        ILogger<StripeBonixWebhookController> logger)
    {
        _options = options.Get("StripeBonix");
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
            _logger.LogWarning(ex, "Invalid Stripe Bonix signature");
            return BadRequest();
        }

        try
        {
            await _webhookService.ProcessEventAsync(stripeEvent, payload, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing Stripe Bonix webhook event {EventId}", stripeEvent.Id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }
}
