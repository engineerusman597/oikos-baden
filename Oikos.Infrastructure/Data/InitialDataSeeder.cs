using System.Security.Cryptography;
using Oikos.Domain.Constants;
using Oikos.Domain.Entities.Invoice;
using Oikos.Domain.Entities.Rbac;
using Oikos.Domain.Entities.Setting;
using Oikos.Domain.Entities.Subscription;
using Oikos.Domain.Enums;
using Oikos.Common.Helpers;
using Oikos.Common.Constants;

namespace Oikos.Infrastructure.Data;

public static class InitialDataSeeder
{
    public static void Seed(OikosDbContext dbContext)
    {
        if (!dbContext.Settings.Any())
        {
            SeedSettings(dbContext);
        }

        EnsureRoles(dbContext);

        if (!dbContext.Users.Any())
        {
            SeedAdminUser(dbContext);
        }

        if (!dbContext.InvoiceStages.Any())
        {
            SeedInvoiceStages(dbContext);
        }

        if (!dbContext.SubscriptionPlans.Any())
        {
            SeedSubscriptionPlans(dbContext);
        }

        var testUser = dbContext.Users.FirstOrDefault(u => u.Email == "test@ModernPaper.de");
        if (testUser == null)
        {
            SeedTestUserWithSubscription(dbContext);
        }
        else
        {
            EnsureTestUserSubscriptionLinked(dbContext, testUser);
        }
    }

    private static void SeedSettings(OikosDbContext dbContext)
    {
        var rsa = RSA.Create();
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

        dbContext.Settings.AddRange(new Setting
        {
            Key = JwtConstant.JwtIssue,
            Value = "Oikos"
        }, new Setting
        {
            Key = JwtConstant.JwtAudience,
            Value = "Admin"
        }, new Setting
        {
            Key = JwtConstant.JwtSigningRsaPrivateKey,
            Value = privateKey
        }, new Setting
        {
            Key = JwtConstant.JwtSigningRsaPublicKey,
            Value = publicKey
        }, new Setting
        {
            Key = JwtConstant.JwtExpireMinute,
            Value = "720"
        });
        dbContext.SaveChanges();
    }

    private static void EnsureRoles(OikosDbContext dbContext)
    {
        var rolesToCheck = new[] { RoleNames.Admin.ToRoleName(), RoleNames.User.ToRoleName(), RoleNames.User_Bonix.ToRoleName() };

        foreach (var roleName in rolesToCheck)
        {
            if (!dbContext.Roles.Any(r => r.Name == roleName))
            {
                dbContext.Roles.Add(new Role { Name = roleName, IsEnabled = true, IsDeleted = false });
            }
        }
        dbContext.SaveChanges();
    }

    private static void SeedAdminUser(OikosDbContext dbContext)
    {
        var adminRole = dbContext.Roles.First(r => r.Name == RoleNames.Admin.ToRoleName());
        var adminUser = new User
        {
            Name = "ModernPaper",
            RealName = "ModernPaper",
            Email = "ModernPaper",
            IsEnabled = true,
            PasswordHash = HashHelper.HashPassword("Start2026!"),
            AcceptedPrivacyPolicy = true,
            PrivacyAcceptedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();

        dbContext.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });
        dbContext.SaveChanges();
    }

    private static void SeedInvoiceStages(OikosDbContext context)
    {
        var now = DateTime.Now;
        var defaultStages = new List<InvoiceStage>
        {
            new() { Name = "Eingang", NameDe = "Eingang", Slug = "eingang", SummaryDe = "Datum, an dem Ihr Antrag oder Ihre Rechnung bei uns eingegangen ist.", PrimaryStatus = InvoicePrimaryStatus.Submitted, DisplayOrder = 1, CreatedAt = now, UpdatedAt = now, Color = "Info" },
            new() { Name = "Status Meldung", NameDe = "Status Meldung", Slug = "status-meldung", SummaryDe = "Allgemeine Statusmeldung zum aktuellen Bearbeitungsstand.", PrimaryStatus = InvoicePrimaryStatus.InReview, DisplayOrder = 2, CreatedAt = now, UpdatedAt = now, Color = "Warning" },
            new() { Name = "Final geprüft", NameDe = "Final geprüft", Slug = "final-geprueft", SummaryDe = "Ihre Unterlagen wurden abschließend geprüft.", PrimaryStatus = InvoicePrimaryStatus.Accepted, DisplayOrder = 3, CreatedAt = now, UpdatedAt = now, Color = "Success" },
            new() { Name = "Angenommene Rechnungen", NameDe = "Angenommene Rechnungen", Slug = "angenommene-rechnungen", SummaryDe = "Rechnungen, die vollständig geprüft und akzeptiert wurden.", PrimaryStatus = InvoicePrimaryStatus.Accepted, DisplayOrder = 4, CreatedAt = now, UpdatedAt = now, Color = "Success" },
            new() { Name = "Mahnantrag", NameDe = "Mahnantrag", Slug = "mahnantrag", SummaryDe = "Antrag auf Erlass eines Mahnbescheids beim zuständigen Gericht.", PrimaryStatus = InvoicePrimaryStatus.Court, DisplayOrder = 5, CreatedAt = now, UpdatedAt = now, Color = "Secondary" },
            new() { Name = "Mahnantrag versendet", NameDe = "Mahnantrag an das zuständige Gericht versendet", Slug = "mahnantrag-versendet", SummaryDe = "Der Mahnantrag wurde an das zuständige Gericht übermittelt.", PrimaryStatus = InvoicePrimaryStatus.Court, DisplayOrder = 6, CreatedAt = now, UpdatedAt = now, Color = "Secondary" },
            new() { Name = "Mahnantrag erhalten", NameDe = "Mahnantrag erfolgreich erhalten", Slug = "mahnantrag-erhalten", SummaryDe = "Das Gericht hat den Mahnantrag erfolgreich erhalten.", PrimaryStatus = InvoicePrimaryStatus.Court, DisplayOrder = 7, CreatedAt = now, UpdatedAt = now, Color = "Secondary" },
            new() { Name = "Frist", NameDe = "Frist", Slug = "frist", SummaryDe = "Frist, innerhalb derer der Schuldner reagieren kann.", PrimaryStatus = InvoicePrimaryStatus.Court, DisplayOrder = 8, CreatedAt = now, UpdatedAt = now, Color = "Error" },
            new() { Name = "Widerspruch eingelegt", NameDe = "Widerspruch eingelegt", Slug = "widerspruch", SummaryDe = "Der Schuldner hat Widerspruch gegen den Mahnbescheid eingelegt.", PrimaryStatus = InvoicePrimaryStatus.Inquiry, DisplayOrder = 9, CreatedAt = now, UpdatedAt = now, Color = "Error" },
            new() { Name = "Vollstreckungsantrag", NameDe = "Vollstreckungsantrag", Slug = "vollstreckungsantrag", SummaryDe = "Antrag auf Zwangsvollstreckung aus dem Titel.", PrimaryStatus = InvoicePrimaryStatus.Court, DisplayOrder = 10, CreatedAt = now, UpdatedAt = now, Color = "Secondary" },
            new() { Name = "Vollstreckungstitel erhalten", NameDe = "Vollstreckungstitel erhalten", Slug = "vollstreckungstitel", SummaryDe = "Der Vollstreckungstitel wurde erteilt und liegt vor.", PrimaryStatus = InvoicePrimaryStatus.Completed, DisplayOrder = 11, CreatedAt = now, UpdatedAt = now, Color = "Dark" },
            new() { Name = "Fallnummer", NameDe = "Fallnummer", Slug = "fallnummer", SummaryDe = "Eindeutige Referenz- bzw. Aktennummer Ihres Falls.", PrimaryStatus = InvoicePrimaryStatus.InReview, DisplayOrder = 12, CreatedAt = now, UpdatedAt = now, Color = "Info" }
        };

        context.InvoiceStages.AddRange(defaultStages);
        context.SaveChanges();
    }

    private static void SeedSubscriptionPlans(OikosDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Slug = "basic-plan",
                Name = "Basic Plan",
                Description = "Basic subscription plan",
                MonthlyPrice = 9m,
                YearlyPrice = 99m,
                MonthlyClaimLimit = 5,
                MonthlyBionicCheckLimit = 0,
                TeamSeatLimit = 0,
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Slug = "standard-plan",
                Name = "Standard Plan",
                Description = "Standard subscription plan",
                MonthlyPrice = 13m,
                YearlyPrice = 149m,
                MonthlyClaimLimit = 10,
                MonthlyBionicCheckLimit = 3,
                TeamSeatLimit = 3,
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Slug = "pro-plan",
                Name = "Pro Plan",
                Description = "Pro subscription plan",
                MonthlyPrice = 20m,
                YearlyPrice = 229m,
                MonthlyClaimLimit = 15,
                MonthlyBionicCheckLimit = 5,
                TeamSeatLimit = null,
                DisplayOrder = 3,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.SubscriptionPlans.AddRange(plans);
        dbContext.SaveChanges();
    }

    private static void SeedTestUserWithSubscription(OikosDbContext dbContext)
    {
        var userRole = dbContext.Roles.First(r => r.Name == RoleNames.User.ToRoleName());
        var plan = dbContext.SubscriptionPlans.First(p => p.Slug == "standard-plan");
        var now = DateTime.UtcNow;

        // 1. Create User
        var testUser = new User
        {
            Name = "TestUser",
            RealName = "Test User",
            Email = "test@ModernPaper.de",
            IsEnabled = true,
            PasswordHash = HashHelper.HashPassword("Test1234!"),
            AcceptedPrivacyPolicy = true,
            PrivacyAcceptedAt = now
        };

        dbContext.Users.Add(testUser);
        dbContext.SaveChanges();

        // 2. Assign Role
        dbContext.UserRoles.Add(new UserRole
        {
            UserId = testUser.Id,
            RoleId = userRole.Id
        });

        // 3. Create Mock Stripe Token
        var stripePayment = new StripePayment
        {
            Email = testUser.Email,
            Name = testUser.RealName,
            StripeSubscriptionId = "sub_test_1234567890", // Mock ID
            StripeCustomerId = "cus_test_1234567890", // Mock ID
            RawPayload = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.StripePayments.Add(stripePayment);
        dbContext.SaveChanges();

        // 4. Create Subscription
        var subscription = new Subscription
        {
            StripePaymentId = stripePayment.Id,
            SubscriptionPlanId = plan.Id,
            SubscriptionType = "standard-plan",
            PurchaseDate = now,
            ExpirationDate = now.AddMonths(1),
            BillingInterval = "monthly",
            StripeSubscriptionId = stripePayment.StripeSubscriptionId,
            PriceAmount = (long)(plan.MonthlyPrice * 100),
            PriceCurrency = "eur",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Subscriptions.Add(subscription);
        dbContext.SaveChanges();

        // 5. Create UserSubscription
        var userSubscription = new UserSubscription
        {
            UserId = testUser.Id,
            SubscriptionPlanId = plan.Id,
            SubscriptionId = subscription.Id,
            ActivationDate = now,
            ExpirationDate = now.AddMonths(1),
            BillingInterval = "monthly",
            Status = SubscriptionStatusConstants.Active,
            AutoRenew = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.UserSubscriptions.Add(userSubscription);
        dbContext.SaveChanges();
    }

    private static void EnsureTestUserSubscriptionLinked(OikosDbContext dbContext, User user)
    {
        var userSubscription = dbContext.UserSubscriptions
            .FirstOrDefault(s => s.UserId == user.Id && s.Status == SubscriptionStatusConstants.Active);

        if (userSubscription == null || userSubscription.SubscriptionId.HasValue)
        {
            return;
        }

        // Try to find existing StripePayment and Subscription
        var stripePayment = dbContext.StripePayments.FirstOrDefault(p => p.Email == user.Email);
        
        if (stripePayment != null)
        {
            var subscription = dbContext.Subscriptions.FirstOrDefault(s => s.StripePaymentId == stripePayment.Id);
            if (subscription != null)
            {
                userSubscription.SubscriptionId = subscription.Id;
                dbContext.SaveChanges();
                return;
            }
        }

        // If missing, create them (similar to SeedTestUserWithSubscription)
        var plan = dbContext.SubscriptionPlans.First(p => p.Slug == "standard-plan");
        var now = DateTime.UtcNow;

        if (stripePayment == null)
        {
            stripePayment = new StripePayment
            {
                Email = user.Email,
                Name = user.RealName,
                StripeSubscriptionId = "sub_test_repair_" + DateTime.Now.Ticks,
                StripeCustomerId = "cus_test_repair_" + DateTime.Now.Ticks,
                RawPayload = "{}",
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.StripePayments.Add(stripePayment);
            dbContext.SaveChanges();
        }

        var newSubscription = new Subscription
        {
            StripePaymentId = stripePayment.Id,
            SubscriptionPlanId = plan.Id,
            SubscriptionType = "standard-plan",
            PurchaseDate = now,
            ExpirationDate = now.AddMonths(1),
            BillingInterval = "monthly",
            StripeSubscriptionId = stripePayment.StripeSubscriptionId,
            PriceAmount = (long)(plan.MonthlyPrice * 100),
            PriceCurrency = "eur",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Subscriptions.Add(newSubscription);
        dbContext.SaveChanges();

        userSubscription.SubscriptionId = newSubscription.Id;
        dbContext.SaveChanges();
    }
}
