namespace Oikos.Application.Services.Subscription.Models;

public sealed record SubscriptionPlanSummary(
    int Id,
    string Name,
    string Slug,
    string? Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int? MonthlyClaimLimit,
    int? MonthlyBionicCheckLimit,
    int? TeamSeatLimit);

public sealed record SubscriptionPlanDetail(
    int? Id,
    string Name,
    string Slug,
    string? Description,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    int? MonthlyClaimLimit,
    int? MonthlyBionicCheckLimit,
    int? TeamSeatLimit,
    int DisplayOrder,
    bool IsActive);

public sealed record UserSubscriptionSnapshot(
    int? UserSubscriptionId,
    int? SubscriptionId,
    int PlanId,
    string PlanName,
    string PlanSlug,
    string? PlanDescription,
    decimal PlanMonthlyPrice,
    decimal PlanYearlyPrice,
    string BillingInterval,
    DateTime ActivationDate,
    DateTime? ExpirationDate,
    int? MonthlyClaimLimit,
    int? MonthlyBionicCheckLimit,
    int? TeamSeatLimit);

public sealed record ClaimSubmissionCheckResult(
    bool IsAllowed,
    int? Remaining,
    bool HasActiveSubscription,
    bool IsExpired,
    DateTime? ExpirationDate);
