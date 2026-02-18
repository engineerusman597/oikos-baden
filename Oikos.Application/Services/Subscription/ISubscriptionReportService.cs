using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Application.Services.Subscription;

public interface ISubscriptionReportService
{
    Task<IReadOnlyList<SubscriptionPaymentRecord>> GetPaymentsAsync(CancellationToken cancellationToken = default);
}
