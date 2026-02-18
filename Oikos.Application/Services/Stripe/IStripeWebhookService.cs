using Stripe;

namespace Oikos.Application.Services.Stripe;

public interface IStripeWebhookService
{
    Task ProcessEventAsync(Event stripeEvent, string rawPayload, StripeOptions? optionsOverride = null, CancellationToken cancellationToken = default);
}
