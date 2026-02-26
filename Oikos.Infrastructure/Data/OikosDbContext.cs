using Oikos.Domain.Entities.CompanyCheck;
using Oikos.Domain.Entities.Log;
using Oikos.Domain.Entities.Partner;
using Oikos.Domain.Entities.Rbac;
using Oikos.Domain.Entities.Setting;
using Oikos.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;

namespace Oikos.Infrastructure.Data;

public class OikosDbContext : DbContext, IAppDbContext
{
    private readonly IServiceProvider _provider;

    public OikosDbContext(
        DbContextOptions<OikosDbContext> options,
         IServiceProvider provider) : base(options)
    {
        _provider = provider;
    }

    public DbSet<Setting> Settings { get; set; }

    public DbSet<UserSetting> UserSettings { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }

    #region Subscription

    public DbSet<StripePayment> StripePayments { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public DbSet<UserSubscription> UserSubscriptions { get; set; }

    #endregion

    #region Invoice

    public DbSet<Domain.Entities.Invoice.Invoice> Invoices { get; set; }

    public DbSet<Domain.Entities.Invoice.InvoiceStage> InvoiceStages { get; set; }

    public DbSet<Domain.Entities.Invoice.InvoiceStageHistory> InvoiceStageHistories { get; set; }

    public DbSet<Domain.Entities.Invoice.InvoiceClientDocument> InvoiceClientDocuments { get; set; }

    #endregion

    #region Company Check

    public DbSet<CompanyCheckRequest> CompanyCheckRequests { get; set; }

    #endregion

    #region Partners

    public DbSet<Partner> Partners { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


    }
}

