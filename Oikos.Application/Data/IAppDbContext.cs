using Oikos.Domain.Entities.CompanyCheck;
using Oikos.Domain.Entities.Invoice;
using Oikos.Domain.Entities.Log;
using Oikos.Domain.Entities.Partner;
using Oikos.Domain.Entities.Rbac;
using Oikos.Domain.Entities.Setting;
using Oikos.Domain.Entities.Subscription;
using Oikos.Domain.Entities.TaxOffice;
using Microsoft.EntityFrameworkCore;

namespace Oikos.Application.Data;

public interface IAppDbContext : IDisposable, IAsyncDisposable
{
    DbSet<Setting> Settings { get; set; }
    DbSet<UserSetting> UserSettings { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<UserPermission> UserPermissions { get; set; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    DbSet<LoginLog> LoginLogs { get; set; }

    // Subscription
    DbSet<StripePayment> StripePayments { get; set; }
    DbSet<Subscription> Subscriptions { get; set; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    DbSet<UserSubscription> UserSubscriptions { get; set; }

    // Invoice
    DbSet<Invoice> Invoices { get; set; }
    DbSet<InvoiceStage> InvoiceStages { get; set; }
    DbSet<InvoiceStageHistory> InvoiceStageHistories { get; set; }
    DbSet<InvoiceClientDocument> InvoiceClientDocuments { get; set; }

    // Company Check
    DbSet<CompanyCheckRequest> CompanyCheckRequests { get; set; }

    // Partners
    DbSet<Partner> Partners { get; set; }

    // Tax Offices
    DbSet<TaxOffice> TaxOffices { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}