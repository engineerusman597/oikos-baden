using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Application.Services.Subscription;

public class SubscriptionReportService : ISubscriptionReportService
{
    private readonly IAppDbContextFactory _dbFactory;

    public SubscriptionReportService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<SubscriptionPaymentRecord>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var users = await context.Users
            .Where(u => u.Email != null)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.RealName,
                UserName = u.Name
            })
            .ToListAsync(cancellationToken);

        var userLookup = users
            .GroupBy(u => u.Email!.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var subscriptions = await context.Subscriptions
            .Include(s => s.StripePayment)
            .Include(s => s.SubscriptionPlan)
            .OrderByDescending(s => s.PurchaseDate)
            .ToListAsync(cancellationToken);

        var records = subscriptions.Select(s =>
        {
            userLookup.TryGetValue(s.StripePayment.Email.ToLowerInvariant(), out var registeredUser);

            return new SubscriptionPaymentRecord(
                s.Id,
                s.StripePayment.Email,
                s.StripePayment.Name,
                s.SubscriptionPlan?.Name ?? s.SubscriptionType,
                s.SubscriptionPlan?.Slug ?? s.SubscriptionType,
                s.BillingInterval,
                s.PurchaseDate,
                s.ExpirationDate,
                s.PriceAmount,
                s.PriceCurrency,
                registeredUser != null,
                registeredUser?.RealName ?? registeredUser?.UserName,
                registeredUser?.Id);
        }).ToList();

        return records;
    }
}
