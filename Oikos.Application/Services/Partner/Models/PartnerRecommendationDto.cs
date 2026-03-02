namespace Oikos.Application.Services.Partner.Models;

public sealed record PartnerRecommendationDto(
    int UserId,
    string Name,
    string? CustomerNumber,
    string? PlanName,
    string SubscriptionStatus,
    string BillingInterval,
    DateTime? ExpirationDate);
