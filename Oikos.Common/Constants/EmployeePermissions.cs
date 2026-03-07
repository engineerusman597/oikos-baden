namespace Oikos.Common.Constants;

public static class EmployeePermissions
{
    // --- User Portal ---
    public const string Dashboard = "dashboard";
    public const string Invoices = "invoices";
    public const string NewCase = "new_case";
    public const string CreditCheck = "credit_check";
    public const string History = "history";

    // --- System (Admin) ---
    public const string AdminHome = "admin_home";
    public const string AdminInvoices = "admin_invoices";
    public const string AdminUsers = "admin_users";
    public const string AdminClients = "admin_clients";
    public const string AdminSubscriptionPayments = "admin_subscription_payments";
    public const string AdminCreditCheck = "admin_credit_check";
    public const string AdminHistory = "admin_history";
    public const string AdminSubscriptions = "admin_subscriptions";
    public const string AdminInvoiceStages = "admin_invoice_stages";
    public const string AdminSettings = "admin_settings";
    public const string AdminPartners = "admin_partners";
    public const string AdminInsurancePartners = "admin_insurance_partners";
    public const string AdminPartnerBanks = "admin_partner_banks";
    public const string AdminTaxOffices = "admin_tax_offices";

    public static readonly IReadOnlyList<string> UserPortalPermissions =
        new[] { Dashboard, Invoices, NewCase, CreditCheck, History };

    public static readonly IReadOnlyList<string> SystemPermissions =
        new[]
        {
            AdminHome, AdminInvoices, AdminUsers, AdminClients, AdminSubscriptionPayments,
            AdminCreditCheck, AdminHistory, AdminSubscriptions, AdminInvoiceStages,
            AdminSettings, AdminPartners, AdminInsurancePartners, AdminPartnerBanks, AdminTaxOffices
        };

    public static readonly IReadOnlyList<string> All =
        UserPortalPermissions.Concat(SystemPermissions).ToList();
}
