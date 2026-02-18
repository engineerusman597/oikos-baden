using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Domain.Entities.Subscription;
using Oikos.Application.Services.Subscription.Models;

namespace Oikos.Application.Services.Subscription;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly IAppDbContextFactory _dbFactory;

    public SubscriptionPlanService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task EnsureSeedPlansAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (await context.SubscriptionPlans.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var basicPlan = new SubscriptionPlan
        {
            Name = "Basic",
            Slug = "basic",
            Description = "Basisplan für einzelne Nutzer",
            MonthlyPrice = 99,
            YearlyPrice = 99 * 12,
            MonthlyClaimLimit = 5,
            MonthlyBionicCheckLimit = 0,
            TeamSeatLimit = 1,
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var standardPlan = new SubscriptionPlan
        {
            Name = "Standard",
            Slug = "standard",
            Description = "Erweitertes Paket mit Teamnutzung",
            MonthlyPrice = 149,
            YearlyPrice = 149 * 12,
            MonthlyClaimLimit = 10,
            MonthlyBionicCheckLimit = 2,
            TeamSeatLimit = null,
            DisplayOrder = 2,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var proPlan = new SubscriptionPlan
        {
            Name = "Pro",
            Slug = "pro",
            Description = "Volle Leistung mit API-Zugriff",
            MonthlyPrice = 229,
            YearlyPrice = 229 * 12,
            MonthlyClaimLimit = 15,
            MonthlyBionicCheckLimit = 5,
            TeamSeatLimit = null,
            DisplayOrder = 3,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.SubscriptionPlans.AddRange(basicPlan, standardPlan, proPlan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlanSummary>> GetPlansAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var plans = await context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        return plans.Select(p => MapPlan(p)).ToList();
    }

    public async Task<IReadOnlyList<SubscriptionPlanDetail>> GetPlansForManagementAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var plans = await context.SubscriptionPlans
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        return plans.Select(MapPlanDetail).ToList();
    }

    public async Task<SubscriptionPlanDetail> SavePlanAsync(SubscriptionPlanDetail plan, CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        SubscriptionPlan entity;

        if (plan.Id.HasValue)
        {
            entity = await context.SubscriptionPlans
                .FirstAsync(p => p.Id == plan.Id.Value, cancellationToken);
        }
        else
        {
            entity = new SubscriptionPlan
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.SubscriptionPlans.Add(entity);
        }

        entity.Name = plan.Name;
        entity.Slug = plan.Slug;
        entity.Description = plan.Description;
        entity.MonthlyPrice = plan.MonthlyPrice;
        entity.YearlyPrice = plan.YearlyPrice;
        entity.MonthlyClaimLimit = plan.MonthlyClaimLimit;
        entity.MonthlyBionicCheckLimit = plan.MonthlyBionicCheckLimit;
        entity.TeamSeatLimit = plan.TeamSeatLimit;
        entity.DisplayOrder = plan.DisplayOrder;
        entity.IsActive = plan.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return MapPlanDetail(entity);
    }

    public async Task DeletePlanAsync(int planId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var plan = await context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan == null)
        {
            return;
        }

        context.SubscriptionPlans.Remove(plan);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSubscriptionSnapshot?> GetActiveSubscriptionAsync(int userId, CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var activeSubscription = await context.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.UserId == userId && s.Status.ToLower() == "active")
            .OrderByDescending(s => s.ActivationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription != null)
        {
            return MapSubscription(activeSubscription);
        }

        return null;
    }

    public async Task<UserSubscriptionSnapshot> ActivatePlanAsync(int userId, int planId, string billingInterval, CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var plan = await context.SubscriptionPlans.FirstAsync(p => p.Id == planId, cancellationToken);

        var activeSubscriptions = await context.UserSubscriptions
            .Where(s => s.UserId == userId && s.Status.ToLower() == "active")
            .ToListAsync(cancellationToken);

        foreach (var subscription in activeSubscriptions)
        {
            subscription.Status = "expired";
            subscription.UpdatedAt = now;
            subscription.ExpirationDate ??= now;
        }

        var newSubscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = plan.Id,
            ActivationDate = now,
            ExpirationDate = billingInterval.Equals("yearly", StringComparison.OrdinalIgnoreCase)
                ? now.AddYears(1)
                : now.AddMonths(1),
            BillingInterval = billingInterval,
            Status = "active",
            AutoRenew = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.UserSubscriptions.Add(newSubscription);
        await context.SaveChangesAsync(cancellationToken);

        newSubscription.SubscriptionPlan = plan;

        return MapSubscription(newSubscription);
    }

    public async Task<ClaimSubmissionCheckResult> CheckClaimSubmissionAsync(int userId, int submissionCount, CancellationToken cancellationToken = default)
    {
        await EnsureSeedPlansAsync(cancellationToken);
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var activeSubscription = await context.UserSubscriptions
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.UserId == userId && s.Status.ToLower() == "active")
            .OrderByDescending(s => s.ActivationDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription == null)
        {
            return new ClaimSubmissionCheckResult(false, 0, false, false, null);
        }

        var now = DateTime.UtcNow;
        var expirationDate = activeSubscription.ExpirationDate;
        if (expirationDate.HasValue && expirationDate.Value <= now)
        {
            return new ClaimSubmissionCheckResult(false, 0, true, true, expirationDate);
        }

        var monthlyLimit = activeSubscription.SubscriptionPlan?.MonthlyClaimLimit;
        if (!monthlyLimit.HasValue)
        {
            return new ClaimSubmissionCheckResult(true, null, true, false, expirationDate);
        }

        var periodStart = GetBillingPeriodStart(activeSubscription.ActivationDate, activeSubscription.BillingInterval, now);
        var periodEnd = GetBillingPeriodEnd(periodStart, activeSubscription.BillingInterval);
        var usedCount = await context.Invoices.AsNoTracking()
            .Where(i => i.UserId == userId && i.CreatedAt >= periodStart && i.CreatedAt < periodEnd)
            .CountAsync(cancellationToken);
        var remaining = Math.Max(monthlyLimit.Value - usedCount, 0);
        var isAllowed = submissionCount <= remaining;

        return new ClaimSubmissionCheckResult(isAllowed, remaining, true, false, expirationDate);
    }

    private static DateTime GetBillingPeriodStart(DateTime activationDate, string billingInterval, DateTime nowUtc)
    {
        var interval = billingInterval?.Trim().ToLowerInvariant();
        if (interval == "yearly")
        {
            var yearsSince = nowUtc.Year - activationDate.Year;
            var candidate = activationDate.AddYears(Math.Max(yearsSince, 0));
            if (candidate > nowUtc)
            {
                candidate = candidate.AddYears(-1);
            }

            return candidate;
        }

        var monthsSince = (nowUtc.Year - activationDate.Year) * 12 + (nowUtc.Month - activationDate.Month);
        var monthCandidate = activationDate.AddMonths(Math.Max(monthsSince, 0));
        if (monthCandidate > nowUtc)
        {
            monthCandidate = monthCandidate.AddMonths(-1);
        }

        return monthCandidate;
    }

    private static DateTime GetBillingPeriodEnd(DateTime periodStart, string billingInterval)
    {
        var interval = billingInterval?.Trim().ToLowerInvariant();
        return interval == "yearly"
            ? periodStart.AddYears(1)
            : periodStart.AddMonths(1);
    }

    private static SubscriptionPlanSummary MapPlan(SubscriptionPlan plan)
    {
        return new SubscriptionPlanSummary(
            plan.Id,
            plan.Name,
            plan.Slug,
            plan.Description,
            plan.MonthlyPrice,
            plan.YearlyPrice,
            plan.MonthlyClaimLimit,
            plan.MonthlyBionicCheckLimit,
            plan.TeamSeatLimit);
    }

    private static SubscriptionPlanDetail MapPlanDetail(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDetail(
            plan.Id,
            plan.Name,
            plan.Slug,
            plan.Description,
            plan.MonthlyPrice,
            plan.YearlyPrice,
            plan.MonthlyClaimLimit,
            plan.MonthlyBionicCheckLimit,
            plan.TeamSeatLimit,
            plan.DisplayOrder,
            plan.IsActive);
    }

    private static UserSubscriptionSnapshot MapSubscription(UserSubscription subscription)
    {
        return new UserSubscriptionSnapshot(
            subscription.Id,
            subscription.SubscriptionId,
            subscription.SubscriptionPlanId,
            subscription.SubscriptionPlan.Name,
            subscription.SubscriptionPlan.Slug,
            subscription.SubscriptionPlan.Description,
            subscription.SubscriptionPlan.MonthlyPrice,
            subscription.SubscriptionPlan.YearlyPrice,
            subscription.BillingInterval,
            subscription.ActivationDate,
            subscription.ExpirationDate,
            subscription.SubscriptionPlan.MonthlyClaimLimit,
            subscription.SubscriptionPlan.MonthlyBionicCheckLimit,
            subscription.SubscriptionPlan.TeamSeatLimit);
    }
}
