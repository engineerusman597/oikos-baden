using Oikos.Application.Services.Authentication;
using Microsoft.Extensions.Logging;
using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Application.Services.Subscription;

public static class SubscriptionAccessHelper
{
    public static bool IsActiveSubscription(UserSubscriptionSnapshot? subscription)
    {
        if (subscription == null)
        {
            return false;
        }

        return subscription.UserSubscriptionId.HasValue
            && (!subscription.ExpirationDate.HasValue || subscription.ExpirationDate.Value > DateTime.UtcNow);
    }

    public static async Task<SubscriptionAccessResult> EvaluateAccessAsync(
        IAuthenticationService authService,
        ISubscriptionPlanService subscriptionPlanService,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var userId = await authService.GetUserIdAsync();
        if (userId == 0)
        {
            return SubscriptionAccessResult.Unauthenticated;
        }

        var subscription = await subscriptionPlanService.GetActiveSubscriptionAsync(userId, cancellationToken);
        return new SubscriptionAccessResult(userId, subscription, IsActiveSubscription(subscription));
    }


}

public sealed record SubscriptionAccessResult(
    int? UserId,
    UserSubscriptionSnapshot? Subscription,
    bool HasActiveSubscription)
{
    public bool HasUser => UserId.HasValue;

    public static SubscriptionAccessResult Unauthenticated => new(
        null,
        new UserSubscriptionSnapshot(null, null, 0, string.Empty, string.Empty, null, 0, 0, string.Empty, DateTime.MinValue, null, null, null, null),
        false);
}
