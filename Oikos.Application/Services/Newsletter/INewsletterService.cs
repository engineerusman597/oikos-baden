using Oikos.Application.Services.Newsletter.Models;

namespace Oikos.Application.Services.Newsletter;

public interface INewsletterService
{
    Task SubscribeAsync(NewsletterSubscriptionRequest request, CancellationToken cancellationToken = default);
}
