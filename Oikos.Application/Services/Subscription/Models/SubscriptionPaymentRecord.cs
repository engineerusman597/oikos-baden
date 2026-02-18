namespace Oikos.Application.Services.Subscription.Models;

public sealed record SubscriptionPaymentRecord(
    int SubscriptionId,
    string Email,
    string? Name,
    string? PlanName,
    string? PlanSlug,
    string? BillingInterval,
    DateTime PurchaseDate,
    DateTime? ExpirationDate,
    long? PriceAmount,
    string? PriceCurrency,
    bool HasRegisteredUser,
    string? RegisteredUserName,
    int? RegisteredUserId);
