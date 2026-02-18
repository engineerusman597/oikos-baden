using Oikos.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oikos.Application.Constants;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.Email;
using Oikos.Application.Services.Email.Templates;
using Oikos.Application.Services.Subscription;
using Oikos.Domain.Entities.Subscription;
using StripeCheckout = global::Stripe.Checkout;
using StripeModels = global::Stripe;
using SubscriptionEntity = Oikos.Domain.Entities.Subscription.Subscription;

namespace Oikos.Application.Services.Stripe;

public class StripeWebhookService : IStripeWebhookService
{
    private readonly IAppDbContextFactory _dbContextFactory;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly IOptionsSnapshot<StripeOptions> _optionsSnapshot;
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IEmailSender _emailSender;
    private readonly ICompanyCheckManager _companyCheckManager;

    public StripeWebhookService(
        IAppDbContextFactory dbContextFactory,
        IOptionsSnapshot<StripeOptions> optionsSnapshot,
        ILogger<StripeWebhookService> logger,
        ISubscriptionPlanService subscriptionPlanService,

        IEmailSender emailSender,
        ICompanyCheckManager companyCheckManager)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _optionsSnapshot = optionsSnapshot;
        _subscriptionPlanService = subscriptionPlanService;
        _emailSender = emailSender;
        _companyCheckManager = companyCheckManager;
    }

    public async Task ProcessEventAsync(
        StripeModels.Event stripeEvent,
        string rawPayload,
        StripeOptions? optionsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var options = optionsOverride ?? _optionsSnapshot.Value;

        if (!string.Equals(stripeEvent.Type, StripeConstants.CheckoutCompletedEvent, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Ignoring Stripe event type {EventType}", stripeEvent.Type);
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Stripe API key is not configured");
        }

        if (stripeEvent.Data.Object is not StripeCheckout.Session session)
        {
            _logger.LogWarning("Stripe event payload did not contain a checkout session");
            return;
        }

        var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Stripe session does not contain a customer email address");
        }

        var normalizedEmail = email.ToLowerInvariant();

        var subscriptionId = session.SubscriptionId ?? session.Subscription?.Id;
        
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            // Check for one-time payment (Company Check)
            if (!string.IsNullOrWhiteSpace(session.ClientReferenceId) && int.TryParse(session.ClientReferenceId, out var requestId))
            {
                await HandleCompanyCheckPaymentAsync(requestId, session.PaymentIntentId ?? session.PaymentIntent?.Id, cancellationToken);
                return;
            }

            throw new InvalidOperationException("Stripe session does not contain a subscription id or valid client reference");
        }

        // Avoid tying outbound Stripe calls to the incoming request cancellation token, which can
        // prematurely cancel the HTTP call and surface as TaskCanceledException.
            var subscriptionService = CreateSubscriptionService(options);
            global::Stripe.Subscription? subscription = await subscriptionService.GetAsync(
                subscriptionId,
                new StripeModels.SubscriptionGetOptions
                {
                    Expand = new List<string> { "items.data.price", "items.data.price.product" }
                },
                cancellationToken: CancellationToken.None);

        var price = subscription?.Items?.Data?.FirstOrDefault()?.Price;
        var subscriptionLabel = ResolvePlanLabel(session, price, subscriptionId);
        var utcNow = DateTime.UtcNow;
        var customerName = session.CustomerDetails?.Name ?? session.Customer?.Name;
        var billingInterval = NormalizeBillingInterval(price?.Recurring?.Interval);

        using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        await _subscriptionPlanService.EnsureSeedPlansAsync(cancellationToken);
        var availablePlans = await dbContext.SubscriptionPlans.ToListAsync(cancellationToken);

        var payment = await dbContext.StripePayments
            .FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId, cancellationToken);
        if (payment == null)
        {
            payment = new StripePayment
            {
                Email = email,
                Name = customerName,
                StripeSubscriptionId = subscriptionId,
                StripeCustomerId = session.CustomerId ?? session.Customer?.Id,
                RawPayload = rawPayload,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            dbContext.StripePayments.Add(payment);
        }
        else
        {
            payment.Name = string.IsNullOrWhiteSpace(customerName) ? payment.Name : customerName;
            payment.StripeCustomerId = string.IsNullOrWhiteSpace(session.CustomerId)
                ? payment.StripeCustomerId
                : session.CustomerId;
            payment.RawPayload = rawPayload;
            payment.UpdatedAt = utcNow;
            dbContext.StripePayments.Update(payment);
        }

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(
            u => (u.Email != null && u.Email.ToLower() == normalizedEmail)
                || (u.Name != null && u.Name.ToLower() == normalizedEmail),
            cancellationToken);

        var subscriptionEntity = await dbContext.Subscriptions.FirstOrDefaultAsync(
            s => s.StripePaymentId == payment.Id,
            cancellationToken);

        if (subscriptionEntity == null)
        {
            subscriptionEntity = new SubscriptionEntity
            {
                StripePayment = payment,
                CreatedAt = utcNow
            };
            dbContext.Subscriptions.Add(subscriptionEntity);
        }

        SubscriptionPlan? plan = null;

        if (subscriptionEntity != null)
        {
            var purchaseDate = session.Created != default
                ? session.Created
                : utcNow;

            DateTime? expirationDate = billingInterval?.Equals("yearly", StringComparison.OrdinalIgnoreCase) == true
                ? purchaseDate.AddYears(1)
                : purchaseDate.AddMonths(1);

            var normalizedExpiration = expirationDate == default
                ? (DateTime?)null
                : expirationDate;

            subscriptionEntity.SubscriptionType = subscriptionLabel;
            subscriptionEntity.PurchaseDate = purchaseDate;
            subscriptionEntity.ExpirationDate = normalizedExpiration;
            subscriptionEntity.StripeSubscriptionId = subscriptionId;
            subscriptionEntity.PriceAmount = price?.UnitAmount;
            subscriptionEntity.PriceCurrency = price?.Currency;
            subscriptionEntity.BillingInterval = billingInterval;
            subscriptionEntity.UpdatedAt = utcNow;

            plan = MatchPlan(subscriptionLabel, availablePlans);
            subscriptionEntity.SubscriptionPlanId = plan?.Id;
            subscriptionEntity.SubscriptionPlan = plan;
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);

        if (existingUser != null && plan != null)
        {
            await _subscriptionPlanService.ActivatePlanAsync(
                existingUser.Id,
                plan.Id,
                NormalizeBillingInterval(billingInterval) ?? "monthly",
                cancellationToken);
        }

        await SendWelcomeEmailAsync(
            email,
            customerName,
            plan?.Name ?? subscriptionLabel,
            existingUser != null);
    }

    private static StripeModels.SubscriptionService CreateSubscriptionService(StripeOptions options)
    {
        var stripeClient = new StripeModels.StripeClient(options.ApiKey);
        return new StripeModels.SubscriptionService(stripeClient);
    }

    private static string? NormalizeBillingInterval(string? interval)
    {
        return interval?.ToLowerInvariant() switch
        {
            "year" => "yearly",
            "annual" => "yearly",
            "month" => "monthly",
            _ => interval
        };
    }

    private static string? ResolvePlanLabel(StripeCheckout.Session session, StripeModels.Price? price, string subscriptionId)
    {
        var metadataPlan = session.Metadata?.GetValueOrDefault("plan_slug")
            ?? session.Metadata?.GetValueOrDefault("plan")
            ?? session.Metadata?.GetValueOrDefault("subscription_plan");

        if (!string.IsNullOrWhiteSpace(metadataPlan))
        {
            return metadataPlan;
        }

        if (!string.IsNullOrWhiteSpace(price?.Nickname))
        {
            return price.Nickname;
        }

        if (!string.IsNullOrWhiteSpace(price?.Product?.Name))
        {
            return price.Product.Name;
        }

        if (!string.IsNullOrWhiteSpace(price?.ProductId))
        {
            return price.ProductId;
        }

        return subscriptionId;
    }

    private static SubscriptionPlan? MatchPlan(string? subscriptionLabel, IReadOnlyCollection<SubscriptionPlan> plans)
    {
        if (string.IsNullOrWhiteSpace(subscriptionLabel) || plans.Count == 0)
        {
            return null;
        }

        var normalized = subscriptionLabel.Trim().ToLowerInvariant();
        return plans.FirstOrDefault(p =>
        {
            var slug = p.Slug.ToLowerInvariant();
            var name = p.Name.ToLowerInvariant();

            return normalized == slug
                || normalized == name
                || normalized.Contains(slug)
                || normalized.Contains(name)
                || slug.Contains(normalized)
                || name.Contains(normalized);
        });
    }

    private async Task SendWelcomeEmailAsync(string email, string? customerName, string? planName, bool hasAccount)
    {
        try
        {
            var subject = SubscriptionWelcomeEmailTemplate.Subject;
            var portalUrl = hasAccount
                ? "https://my.online-mahnantraege.de/login"
                : $"https://my.online-mahnantraege.de/login?register=1&email={Uri.EscapeDataString(email)}";
            var htmlBody = hasAccount
                ? SubscriptionWelcomeEmailTemplate.RenderExistingUser(
                    customerName ?? email,
                    planName ?? "Ihr Paket",
                    portalUrl)
                : SubscriptionWelcomeEmailTemplate.Render(
                    customerName ?? email,
                    planName ?? "Ihr Paket",
                    portalUrl);

            await _emailSender.SendEmailAsync(
                email,
                subject,
                htmlBody,
                customerName,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription welcome email to {Email}", email);
        }
        }


    private async Task HandleCompanyCheckPaymentAsync(int requestId, string? paymentIntentId, CancellationToken cancellationToken)
    {
        await _companyCheckManager.MarkPaymentConfirmedAsync(requestId, paymentIntentId, null, cancellationToken);
    }
}
