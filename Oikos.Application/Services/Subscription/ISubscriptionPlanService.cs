using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Application.Services.Subscription;

public interface ISubscriptionPlanService
{
    Task EnsureSeedPlansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPlanSummary>> GetPlansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPlanDetail>> GetPlansForManagementAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPlanDetail> SavePlanAsync(SubscriptionPlanDetail plan, CancellationToken cancellationToken = default);

    Task DeletePlanAsync(int planId, CancellationToken cancellationToken = default);

    Task<UserSubscriptionSnapshot?> GetActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserSubscriptionSnapshot> ActivatePlanAsync(int userId, int planId, string billingInterval, CancellationToken cancellationToken = default);

    Task<ClaimSubmissionCheckResult> CheckClaimSubmissionAsync(int userId, int submissionCount, CancellationToken cancellationToken = default);
}
