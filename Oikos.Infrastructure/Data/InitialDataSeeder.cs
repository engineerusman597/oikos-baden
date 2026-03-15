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

        UpsertInvoiceStages(dbContext);

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
        var rolesToCheck = new[] { RoleNames.Admin.ToRoleName(), RoleNames.User.ToRoleName(), RoleNames.User_Bonix.ToRoleName(), RoleNames.Partner.ToRoleName(), RoleNames.Employee.ToRoleName() };

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

    private static void UpsertInvoiceStages(OikosDbContext context)
    {
        var now = DateTime.Now;

        // Admin Name = internal/detailed name shown to staff
        // ClientName = simpler client-facing name shown to clients
        var definitions = new[]
        {
            (Slug: "neu",                    Order: 1,  Color: "Info",      Status: InvoicePrimaryStatus.Submitted,          ClientAction: false,
             Name: "New",                           NameDe: "Neu",
             ClientName: "Submitted",               ClientNameDe: "Eingereicht",
             SummaryDe: "Ihr Fall ist bei uns eingegangen und wird bearbeitet."),

            (Slug: "in-pruefung",            Order: 2,  Color: "Warning",   Status: InvoicePrimaryStatus.InReview,           ClientAction: false,
             Name: "In Review",                     NameDe: "In Prüfung",
             ClientName: "Under Review",            ClientNameDe: "In Prüfung",
             SummaryDe: "Ihr Fall wird derzeit von unserem Team geprüft."),

            (Slug: "akzeptiert",             Order: 3,  Color: "Success",   Status: InvoicePrimaryStatus.Accepted,           ClientAction: false,
             Name: "Accepted – Court Preparation",  NameDe: "Akzeptiert – Vorbereitung Gericht",
             ClientName: "Accepted",                ClientNameDe: "Akzeptiert",
             SummaryDe: "Ihr Fall wurde akzeptiert. Wir bereiten den Mahnantrag vor."),

            (Slug: "mahnantrag-erstellt",    Order: 4,  Color: "Secondary", Status: InvoicePrimaryStatus.Court,              ClientAction: false,
             Name: "Dunning Request Created",        NameDe: "Mahnantrag erstellt",
             ClientName: "Dunning Request Created",  ClientNameDe: "Mahnantrag erstellt",
             SummaryDe: "Der Mahnantrag wurde erstellt und wird vorbereitet."),

            (Slug: "mahnantrag-gesendet",    Order: 5,  Color: "Secondary", Status: InvoicePrimaryStatus.Court,              ClientAction: false,
             Name: "Dunning Request Sent to Court",  NameDe: "Mahnantrag an Gericht gesendet",
             ClientName: "Dunning Request Sent",     ClientNameDe: "Mahnantrag an Gericht gesendet",
             SummaryDe: "Der Mahnantrag wurde an das zuständige Gericht übermittelt."),

            (Slug: "mahnbescheid-erhalten",  Order: 6,  Color: "Secondary", Status: InvoicePrimaryStatus.Court,              ClientAction: false,
             Name: "Dunning Notice Received",        NameDe: "Mahnbescheid erhalten",
             ClientName: "Dunning Notice Received",  ClientNameDe: "Mahnbescheid erhalten",
             SummaryDe: "Das Gericht hat den Mahnbescheid erlassen."),

            (Slug: "frist-gesetzt",          Order: 7,  Color: "Error",     Status: InvoicePrimaryStatus.DeadlineRunning,    ClientAction: false,
             Name: "Deadline Set",                   NameDe: "Frist gesetzt",
             ClientName: "Objection Period Running", ClientNameDe: "Widerspruchsfrist läuft",
             SummaryDe: "Die Widerspruchsfrist für den Schuldner läuft."),

            (Slug: "akte-an-anwalt",         Order: 8,  Color: "Error",     Status: InvoicePrimaryStatus.Inquiry,            ClientAction: false,
             Name: "File Forwarded to Lawyer",       NameDe: "Akte an Anwalt",
             ClientName: "File Forwarded to Lawyer", ClientNameDe: "Akte an Anwalt",
             SummaryDe: "Der Schuldner hat Widerspruch eingelegt. Die Akte wurde an einen Anwalt übergeben."),

            (Slug: "vollstreckung-beantragen", Order: 9, Color: "Warning",  Status: InvoicePrimaryStatus.EnforcementReady,   ClientAction: true,
             Name: "Waiting for Enforcement Application", NameDe: "Warten auf Vollstreckungsantrag",
             ClientName: "Commission Enforcement",  ClientNameDe: "Vollstreckung beantragen",
             SummaryDe: "Bitte beauftragen Sie jetzt die Vollstreckung."),

            (Slug: "vollstreckung-beantragt", Order: 10, Color: "Secondary", Status: InvoicePrimaryStatus.EnforcementReady,  ClientAction: false,
             Name: "Enforcement Application Filed",  NameDe: "Vollstreckung beantragt",
             ClientName: "Enforcement Filed",        ClientNameDe: "Vollstreckung beantragt",
             SummaryDe: "Der Vollstreckungsantrag wurde gestellt."),

            (Slug: "vollstreckung-laeuft",   Order: 11, Color: "Dark",      Status: InvoicePrimaryStatus.EnforcementInProgress, ClientAction: false,
             Name: "Enforcement in Progress",        NameDe: "Vollstreckung läuft",
             ClientName: "Enforcement in Progress",  ClientNameDe: "Vollstreckung läuft",
             SummaryDe: "Die Zwangsvollstreckung ist eingeleitet.")
        };

        var newSlugs = definitions.Select(d => d.Slug).ToHashSet();

        // Push any old/unknown stages out of the way (DisplayOrder 100+)
        foreach (var old in context.InvoiceStages.Where(s => !newSlugs.Contains(s.Slug)))
        {
            if (old.DisplayOrder < 100)
                old.DisplayOrder += 100;
        }

        foreach (var def in definitions)
        {
            var existing = context.InvoiceStages.FirstOrDefault(s => s.Slug == def.Slug);
            if (existing != null)
            {
                existing.Name = def.Name;
                existing.NameDe = def.NameDe;
                existing.ClientName = def.ClientName;
                existing.ClientNameDe = def.ClientNameDe;
                existing.SummaryDe = def.SummaryDe;
                existing.PrimaryStatus = def.Status;
                existing.DisplayOrder = def.Order;
                existing.Color = def.Color;
                existing.RequiresClientAction = def.ClientAction;
                existing.UpdatedAt = now;
            }
            else
            {
                context.InvoiceStages.Add(new InvoiceStage
                {
                    Name = def.Name,
                    NameDe = def.NameDe,
                    ClientName = def.ClientName,
                    ClientNameDe = def.ClientNameDe,
                    Slug = def.Slug,
                    SummaryDe = def.SummaryDe,
                    PrimaryStatus = def.Status,
                    DisplayOrder = def.Order,
                    Color = def.Color,
                    RequiresClientAction = def.ClientAction,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

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
