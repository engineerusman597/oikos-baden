namespace Oikos.Application.Services.Stripe;

public class StripeOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}
