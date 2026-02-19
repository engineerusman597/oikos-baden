using Oikos.Domain.Entities.Rbac;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Common;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Registration.Models;
using Oikos.Application.Services.Security;
using Oikos.Common.Constants;
using Oikos.Common.Helpers;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Common.Resources;
using Oikos.Application.Services.Email;
using Oikos.Application.Services.Email.Templates;
using Microsoft.Extensions.Options;

namespace Oikos.Application.Services.Registration;

public class RegistrationService : IRegistrationService
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly ISubscriptionPlanService _subscriptionPlanService;
    private readonly IPartnerService _partnerService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailBonixOptions;

    public RegistrationService(
        IAppDbContextFactory dbFactory,
        ISubscriptionPlanService subscriptionPlanService,
        IPartnerService partnerService,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptionsSnapshot<EmailOptions> emailOptions)
    {
        _dbFactory = dbFactory;
        _subscriptionPlanService = subscriptionPlanService;
        _partnerService = partnerService;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _emailBonixOptions = emailOptions.Get("EmailBonix");
    }

    public async Task<RegistrationResult> RegisterUserAsync(RegisterUserRequest request)
    {
        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Normalize and validate input
            var email = request.Email.Trim();
            var normalizedEmail = email.ToLowerInvariant();
            var company = request.Company?.Trim();
            var firstName = request.FirstName?.Trim();
            var lastName = request.LastName?.Trim();
            var gender = NameHelper.NormalizeGender(request.Gender) ?? request.Gender;
            var title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title?.Trim();
            var partnerCode = request.PartnerCode?.Trim().ToUpperInvariant();

            // Validation
            if (string.IsNullOrWhiteSpace(email))
            {
                return new RegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = SharedResource.ResourceManager.GetString("Register_EmailRequired") ?? "Email is required" 
                };
            }

            // Check if user exists
            var exists = await context.Users.AnyAsync(u =>
                (u.Name != null && u.Name.ToLower() == normalizedEmail) ||
                (u.Email != null && u.Email.ToLower() == normalizedEmail));
            
            if (exists)
            {
                return new RegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = SharedResource.ResourceManager.GetString("Register_UserExists") ?? "User already exists" 
                };
            }

            // Check for purchases/subscriptions (Only for Rechtfix users)
            Oikos.Domain.Entities.Subscription.Subscription? latestSubscription = null;
            SubscriptionPlanDetail? planToActivate = null;
            string? billingInterval = null;

            if (!request.IsBonixUser && !request.SkipSubscriptionCheck)
            {
                var purchases = await context.StripePayments
                    .Include(u => u.Subscriptions!)
                        .ThenInclude(s => s.SubscriptionPlan!)
                    .Where(u => u.Email.ToLower() == normalizedEmail)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                latestSubscription = purchases
                    .SelectMany(p => p.Subscriptions ?? Enumerable.Empty<Domain.Entities.Subscription.Subscription>())
                    .Where(s => !s.ExpirationDate.HasValue || s.ExpirationDate.Value > now)
                    .OrderByDescending(s => s.PurchaseDate == default ? s.CreatedAt : s.PurchaseDate)
                    .FirstOrDefault();

                if (latestSubscription == null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = SharedResource.ResourceManager.GetString("Register_ActiveSubscriptionRequired") ?? "Active subscription required"
                    };
                }

                // Get subscription plan
                var plans = await _subscriptionPlanService.GetPlansForManagementAsync();
                planToActivate = MatchPlan(latestSubscription, plans)
                    ?? plans.OrderBy(p => p.DisplayOrder).FirstOrDefault();

                if (planToActivate?.Id == null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = SharedResource.ResourceManager.GetString("Register_SubscriptionPlanNotFound") ?? "Subscription plan not found"
                    };
                }

                billingInterval = NormalizeBillingInterval(latestSubscription.BillingInterval);
            }
            
            var customerNumber = await CustomerNumberHelper.GenerateUniqueCustomerNumberAsync(context);

            // Validate partner code
            var partner = string.IsNullOrWhiteSpace(partnerCode)
                ? null
                : await _partnerService.GetByCodeAsync(partnerCode);

            if (!string.IsNullOrWhiteSpace(partnerCode) && partner == null)
            {
                return new RegistrationResult 
                { 
                    Success = false, 
                    ErrorMessage = SharedResource.ResourceManager.GetString("Register_InvalidPartnerCode") ?? "Invalid partner code" 
                };
            }

            // Create user
            var user = new Domain.Entities.Rbac.User
            {
                Name = email,
                RealName = NameHelper.ComposeFullName(title, firstName, lastName, null),
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsEnabled = true,
                IsDeleted = false,
                IsSpecial = false,
                Company = company,
                Gender = gender,
                AcademicTitle = title,
                CustomerNumber = customerNumber,
                PartnerId = partner?.Id,
                AcceptedPrivacyPolicy = request.AcceptedPrivacy,
                PrivacyAcceptedAt = request.AcceptedPrivacy ? DateTime.UtcNow : null
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Assign role
            var roleName = request.IsBonixUser ? RoleNames.User_Bonix.ToRoleName() : RoleNames.User.ToRoleName();
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            
            if (userRole == null)
            {
                 // Fallback to "User" if "User_Bonix" not found (should be seeded though)
                 userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.User.ToRoleName());
            }

            if (userRole != null)
            {
                context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = userRole.Id
                });
                await context.SaveChangesAsync();
            }

            // Activate subscription (Only if plan identified)
            if (planToActivate?.Id != null && billingInterval != null)
            {
                await _subscriptionPlanService.ActivatePlanAsync(user.Id, planToActivate.Id.Value, billingInterval);
            }

            // Send notification email to support for Bonix registrations
            if (request.IsBonixUser)
            {
                try
                {
                    var subject = BonixRegistrationNotificationTemplate.Subject;
                    var body = BonixRegistrationNotificationTemplate.Render(
                        company ?? "Nicht angegeben",
                        email,
                        DateTime.Now
                    );

                    var supportEmail = _emailBonixOptions.SupportEmail ?? "support@bonix-auskunft.de";
                    await _emailSender.SendEmailAsync(
                        supportEmail,
                        subject,
                        body,
                        "Bonix Support",
                        attachments: null,
                        EmailConfigurationType.Bonix
                    );
                }
                catch (Exception)
                {
                    // Log but don't fail registration if notification fails
                    // Registration is successful even if notification email fails
                }

                // Send welcome email to the user
                try
                {
                    var welcomeSubject = BonixWelcomeEmailTemplate.Subject;
                    var welcomeBody = BonixWelcomeEmailTemplate.Render(user.RealName);

                    await _emailSender.SendEmailAsync(
                        email,
                        welcomeSubject,
                        welcomeBody,
                        user.RealName,
                        attachments: null,
                        EmailConfigurationType.Bonix
                    );
                }
                catch (Exception)
                {
                    // Log but don't fail registration if welcome email fails
                }
            }

            return new RegistrationResult
            {
                Success = true,
                UserName = email,
                Password = request.Password,
                UserId = user.Id
            };
        }
        catch (Exception ex)
        {
            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }





    private static SubscriptionPlanDetail? MatchPlan(
        Domain.Entities.Subscription.Subscription subscription,
        IReadOnlyList<SubscriptionPlanDetail> plans)
    {
        var labels = new[]
        {
            subscription.SubscriptionPlan?.Slug,
            subscription.SubscriptionPlan?.Name,
            subscription.SubscriptionType
        }
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .Select(l => l!.Trim().ToLowerInvariant())
        .ToArray();

        return plans.FirstOrDefault(p => labels.Any(l =>
            p.Slug.ToLowerInvariant() == l ||
            p.Name.ToLowerInvariant() == l));
    }

    private static string NormalizeBillingInterval(string? interval)
    {
        return interval?.ToLowerInvariant() switch
        {
            "year" => "yearly",
            "annual" => "yearly",
            "month" => "monthly",
            "monthly" => "monthly",
            "yearly" => "yearly",
            _ => "monthly"
        };
    }
}
