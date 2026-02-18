using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyCheckRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User identifier"),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, comment: "Original company name provided by user"),
                    CreditsafeCompanyId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, comment: "Creditsafe company identifier"),
                    CreditsafeCountryCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true, comment: "Creditsafe country code"),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, comment: "Request status"),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, comment: "Charged amount"),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, comment: "Currency code"),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Customer email for Stripe"),
                    SelectedCompanyJson = table.Column<string>(type: "TEXT", nullable: true, comment: "Serialized summary of selected company"),
                    ReportJson = table.Column<string>(type: "TEXT", nullable: true, comment: "Serialized detailed report"),
                    ReportPdfPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Relative path of the generated PDF report"),
                    ReportDownloadToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, comment: "Unique download token for the report PDF"),
                    ReportPdfGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Timestamp when the PDF report was generated"),
                    StripeCheckoutSessionId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Stripe checkout session identifier"),
                    StripePaymentIntentId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Stripe payment intent identifier"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Request creation time"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Last update time"),
                    PaymentConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Payment confirmation time")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCheckRequests", x => x.Id);
                },
                comment: "Company check requests and reports");

            migrationBuilder.CreateTable(
                name: "InvoiceStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false, comment: "Stage display name"),
                    NameDe = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true, comment: "Stage display name (German)"),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false, comment: "Route slug for navigation"),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true, comment: "Short summary shown in navigation"),
                    SummaryDe = table.Column<string>(type: "TEXT", nullable: true, comment: "Short summary shown in navigation (German)"),
                    Description = table.Column<string>(type: "TEXT", nullable: true, comment: "Detailed description of the stage"),
                    DescriptionDe = table.Column<string>(type: "TEXT", nullable: true, comment: "Detailed description of the stage (German)"),
                    NextSteps = table.Column<string>(type: "TEXT", nullable: true, comment: "Recommended next steps for the user"),
                    NextStepsDe = table.Column<string>(type: "TEXT", nullable: true, comment: "Recommended next steps for the user (German)"),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true, comment: "MudBlazor icon identifier"),
                    Color = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true, comment: "Preferred MudBlazor color"),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false, comment: "Display order"),
                    PrimaryStatus = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary status classification"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Create time"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Update time")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStages", x => x.Id);
                },
                comment: "Invoice workflow stages");

            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false, comment: "Login name"),
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Login time"),
                    Agent = table.Column<string>(type: "TEXT", nullable: true, comment: "Login client"),
                    Ip = table.Column<string>(type: "TEXT", nullable: true, comment: "Login IP"),
                    IsSuccessd = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Was successful")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                },
                comment: "Login log");

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, comment: "Partner display name"),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, comment: "Unique referral code"),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Primary contact email"),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Internal notes"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation timestamp (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                },
                comment: "Lead partners");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true, comment: "Role name"),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Is enabled"),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Is deleted")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                },
                comment: "Role");

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false, comment: "Key"),
                    Value = table.Column<string>(type: "TEXT", nullable: false, comment: "Value")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                },
                comment: "System setting");

            migrationBuilder.CreateTable(
                name: "StripePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false, comment: "Customer email address"),
                    Name = table.Column<string>(type: "TEXT", nullable: true, comment: "Customer full name provided by Stripe"),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", nullable: false, comment: "Stripe subscription id associated with the checkout session"),
                    StripeCustomerId = table.Column<string>(type: "TEXT", nullable: true, comment: "Stripe customer id provided during checkout"),
                    RawPayload = table.Column<string>(type: "TEXT", nullable: false, comment: "Raw webhook payload for auditing"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Created timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripePayments", x => x.Id);
                },
                comment: "Raw Stripe webhook data captured for purchases");

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", nullable: false, comment: "Unique slug for internal references"),
                    Name = table.Column<string>(type: "TEXT", nullable: false, comment: "Display name shown to users"),
                    Description = table.Column<string>(type: "TEXT", nullable: true, comment: "Plan description"),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false, comment: "Monthly price in euros"),
                    YearlyPrice = table.Column<decimal>(type: "TEXT", nullable: false, comment: "Yearly price in euros"),
                    MonthlyClaimLimit = table.Column<int>(type: "INTEGER", nullable: true, comment: "Monthly claim submission limit"),
                    MonthlyBionicCheckLimit = table.Column<int>(type: "INTEGER", nullable: true, comment: "Monthly bionic company check limit"),
                    TeamSeatLimit = table.Column<int>(type: "INTEGER", nullable: true, comment: "Maximum team members allowed; null means unlimited"),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false, comment: "Display ordering"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Whether the plan is available for purchase"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                },
                comment: "Defines a purchasable subscription plan with feature limits");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User ID"),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Role ID")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                },
                comment: "User role");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User ID"),
                    Key = table.Column<string>(type: "TEXT", nullable: false, comment: "Key"),
                    Value = table.Column<string>(type: "TEXT", nullable: false, comment: "Value")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                },
                comment: "User setting");

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Owner User Id"),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, comment: "Stored file path (relative to web root)"),
                    PowerOfAttorneyPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Stored power of attorney file path (relative to web root)"),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Company name"),
                    Amount = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Amount as provided"),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Invoice date"),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, comment: "Currency code"),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true, comment: "Description"),
                    TicketNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true, comment: "Ticket number"),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Current workflow stage identifier"),
                    PrimaryStatus = table.Column<int>(type: "INTEGER", nullable: false, comment: "Cached current primary status"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Create time"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Update time")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_InvoiceStages_StageId",
                        column: x => x.StageId,
                        principalTable: "InvoiceStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "User Invoices");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Avatar = table.Column<string>(type: "TEXT", nullable: true, comment: "User avatar"),
                    Name = table.Column<string>(type: "TEXT", nullable: false, comment: "Username"),
                    RealName = table.Column<string>(type: "TEXT", nullable: false, comment: "Full name"),
                    AcademicTitle = table.Column<string>(type: "TEXT", nullable: true, comment: "Academic title"),
                    Gender = table.Column<string>(type: "TEXT", nullable: true, comment: "Gender"),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false, comment: "Password hash"),
                    Email = table.Column<string>(type: "TEXT", nullable: true, comment: "Email address"),
                    Company = table.Column<string>(type: "TEXT", nullable: true, comment: "Company name"),
                    AcceptedPrivacyPolicy = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Privacy policy accepted"),
                    PrivacyAcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Privacy policy accepted at"),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true, comment: "Phone number"),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Is enabled"),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Is deleted"),
                    IsSpecial = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Is special user"),
                    LoginValiedTime = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Login failure tracking expiry"),
                    CustomerNumber = table.Column<string>(type: "TEXT", nullable: true, comment: "Customer number"),
                    SepaMandatePath = table.Column<string>(type: "TEXT", nullable: true, comment: "Relative path to the latest generated SEPA mandate for the user"),
                    SepaMandateGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Timestamp of the latest generated SEPA mandate"),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true, comment: "Referring partner id")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id");
                },
                comment: "User");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    StripePaymentId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Foreign key to the captured Stripe payment"),
                    SubscriptionPlanId = table.Column<int>(type: "INTEGER", nullable: true, comment: "Linked subscription plan derived from Stripe metadata"),
                    SubscriptionType = table.Column<string>(type: "TEXT", nullable: false, comment: "Subscription type derived from Stripe PriceId"),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Purchase timestamp (UTC)"),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Subscription expiration timestamp (UTC)"),
                    BillingInterval = table.Column<string>(type: "TEXT", nullable: true, comment: "Billing interval reported by Stripe (monthly/yearly)"),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", nullable: true, comment: "Stripe subscription identifier"),
                    PriceAmount = table.Column<long>(type: "INTEGER", nullable: true, comment: "Price amount in the smallest currency unit"),
                    PriceCurrency = table.Column<string>(type: "TEXT", nullable: true, comment: "Three-letter ISO currency code for the subscription price"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_StripePayments_StripePaymentId",
                        column: x => x.StripePaymentId,
                        principalTable: "StripePayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id");
                },
                comment: "Represents a purchased subscription mapped from Stripe checkout sessions");

            migrationBuilder.CreateTable(
                name: "InvoiceStageHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Invoice identifier"),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Stage identifier"),
                    ChangedByUserId = table.Column<int>(type: "INTEGER", nullable: true, comment: "User identifier that performed the change"),
                    ChangedByUserName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Name snapshot of the user that performed the change"),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Change timestamp"),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Optional note about the change")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceStageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceStageHistories_InvoiceStages_StageId",
                        column: x => x.StageId,
                        principalTable: "InvoiceStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceStageHistories_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "History of invoice stage changes");

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User identifier"),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false, comment: "Token hash"),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Expiration time (UTC)"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation time (UTC)"),
                    ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Consumption time (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Password reset tokens");

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User id"),
                    SubscriptionPlanId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Subscribed plan"),
                    ActivationDate = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Activation timestamp (UTC)"),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "Optional expiration timestamp (UTC)"),
                    BillingInterval = table.Column<string>(type: "TEXT", nullable: false, comment: "Billing interval: monthly/yearly"),
                    Status = table.Column<string>(type: "TEXT", nullable: false, comment: "Status: active, cancelled, expired"),
                    AutoRenew = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Auto renew flag"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation timestamp"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Last update timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tracks the active subscription plan for a user");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StageId",
                table: "Invoices",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStageHistories_InvoiceId",
                table: "InvoiceStageHistories",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceStageHistories_StageId",
                table: "InvoiceStageHistories",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_Time",
                table: "LoginLogs",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_UserName",
                table: "LoginLogs",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Code",
                table: "Partners",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripePaymentId",
                table: "Subscriptions",
                column: "StripePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriptionPlanId",
                table: "Subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PartnerId",
                table: "Users",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCheckRequests");

            migrationBuilder.DropTable(
                name: "InvoiceStageHistories");

            migrationBuilder.DropTable(
                name: "LoginLogs");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "StripePayments");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "InvoiceStages");

            migrationBuilder.DropTable(
                name: "Partners");
        }
    }
}
