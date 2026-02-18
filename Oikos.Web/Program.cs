using Cropper.Blazor.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using MudBlazor;
using MudBlazor.Services;
using Oikos.Web;
using Oikos.Web.Auth;
using Infrastructure.Email;
using Oikos.Infrastructure.Data;
using Oikos.Application.Data;
using Oikos.Infrastructure.Security;
using Oikos.Web.States;
using Oikos.Application.Services.Authentication;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Certifier;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Stripe;
using Oikos.Application.Services.Email;
using Oikos.Application.Services.Invoice;
using Oikos.Infrastructure.Services.Invoice;
using Oikos.Infrastructure.BackgroundServices;
using Oikos.Web.Components;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// log
Environment.CurrentDirectory = AppContext.BaseDirectory;
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 320 * 1024;
    })
    .AddInteractiveWebAssemblyComponents();

// mudblazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;

    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 200;
    config.SnackbarConfiguration.ShowTransitionDuration = 200;
    config.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudMarkdownServices();
builder.Services.AddCropper();

var dbDirectory = Path.Combine(AppContext.BaseDirectory, "DB");
if (!Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

// dbcontext
builder.AddDatabase();

// custom auth state provider
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<IAppDbContextFactory, AppDbContextFactory>();
builder.Services.AddScoped<ExternalAuthService>();

// jwt authentication
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAuthorizePolicy", policy =>
    {
        // Require the user to be authenticated
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ApiAuthorizeRequirement());
    });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtOptionsExtension.InitialJwtOptions);

// some service
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<Oikos.Application.Services.Authentication.IAuthenticationService, Oikos.Application.Services.Authentication.AuthenticationService>();
builder.Services.AddScoped<Oikos.Application.Services.Newsletter.INewsletterService, Oikos.Application.Services.Newsletter.NewsletterService>();
builder.Services.AddHttpClient<CertifierVerificationService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<ISubscriptionReportService, SubscriptionReportService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPartnerContentService, PartnerContentService>();
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<StripeOptions>("StripeTest", builder.Configuration.GetSection("Stripe_Test"));
builder.Services.Configure<StripeOptions>("StripeBonix", builder.Configuration.GetSection("Stripe_Bonix"));
builder.Services.Configure<CertifierOptions>(builder.Configuration.GetSection("Certifier"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<EmailOptions>("EmailBonix", builder.Configuration.GetSection("EmailBonix"));
builder.Services.Configure<BonixOptions>(builder.Configuration.GetSection("Bonix"));
builder.Services.AddHttpClient<ICertifierClient, CertifierClient>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<Oikos.Application.Services.Security.IPasswordHasher, Oikos.Infrastructure.Security.PasswordHasher>();
builder.Services.AddScoped<Oikos.Application.Services.Security.IJwtTokenGenerator, Oikos.Infrastructure.Security.JwtTokenGeneratorAdapter>();
builder.Services.AddScoped<Oikos.Application.Services.Registration.IRegistrationService, Oikos.Application.Services.Registration.RegistrationService>();

// CompanyCheck Services
builder.Services.AddHttpClient(Oikos.Infrastructure.Constants.CreditSafeConstants.HttpClientName);
builder.Services.AddScoped<ICreditSafeClient, Oikos.Infrastructure.CompanyCheck.CreditSafeClient>();
builder.Services.AddScoped<Oikos.Infrastructure.CompanyCheck.CompanyCheckReportFormatter>();
builder.Services.AddScoped<ICompanyCheckManager, Oikos.Infrastructure.CompanyCheck.CompanyCheckManager>();
builder.Services.AddScoped<ISepaMandateGenerator, Oikos.Infrastructure.CompanyCheck.SepaMandateGenerator>();
builder.Services.AddScoped<ICompanyCheckWizardService, CompanyCheckWizardService>();

// Invoice Services
builder.Services.AddHostedService<InvoiceOcrBackgroundService>();
builder.Services.AddSingleton<IInvoiceExtractionService, HeuristicInvoiceExtractionService>();
builder.Services.AddSingleton<IPowerOfAttorneyPdfGenerator, PowerOfAttorneyPdfGenerator>();
builder.Services.AddScoped<IInvoiceManagementService, InvoiceManagementService>();
builder.Services.AddScoped<IInvoiceSubmissionService, InvoiceSubmissionService>();

// User and Role Management Services
builder.Services.AddScoped<Oikos.Application.Services.User.IUserManagementService, Oikos.Application.Services.User.UserManagementService>();
builder.Services.AddScoped<Oikos.Application.Services.User.IUserProfileService, Oikos.Application.Services.User.UserProfileService>();
builder.Services.AddScoped<Oikos.Application.Services.User.IAvatarStorageService, Oikos.Infrastructure.Services.User.AvatarStorageService>();
builder.Services.AddScoped<Oikos.Application.Services.Role.IRoleManagementService, Oikos.Application.Services.Role.RoleManagementService>();
builder.Services.AddScoped<Oikos.Application.Services.User.IUserRoleService, Oikos.Application.Services.User.UserRoleService>();
builder.Services.AddScoped<Oikos.Application.Services.User.IUserSettingService, Oikos.Application.Services.User.UserSettingService>();



builder.Services.AddScoped<Oikos.Application.Services.Setting.ISettingService, Oikos.Application.Services.Setting.SettingService>();
builder.Services.AddScoped<Oikos.Application.Services.Dashboard.IDashboardService, Oikos.Application.Services.Dashboard.DashboardService>();

// locallization
builder.Services.AddLocalization();

// get ip and agent only for record login log 
builder.Services.AddHttpContextAccessor();

// jwt helper
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<Oikos.Web.Components.Layout.States.ILayoutState, Oikos.Web.Components.Layout.States.LayoutState>();
builder.Services.AddScoped<Oikos.Web.Components.Layout.States.ILayoutState, Oikos.Web.Components.Layout.States.LayoutState>();
builder.Services.AddScoped<ThemeState>();

builder.Services.AddControllers();



var app = builder.Build();

CurrentApplication.Application = app;
app.InitialDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("de-DE")
    .AddSupportedCultures("de-DE", "en-US")
    .AddSupportedUICultures("de-DE", "en-US");

app.UseRequestLocalization(localizationOptions);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = { IPAddress.Parse("127.0.0.1") }
});
app.UseHttpsRedirection();
app.UseHsts();

var avatarDirectory = Path.Combine(AppContext.BaseDirectory, "Avatars");
if (!Directory.Exists(avatarDirectory))
{
    Directory.CreateDirectory(avatarDirectory);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(avatarDirectory),
    RequestPath = "/Avatars"
});

app.MapStaticAssets();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/company-checks/report/{token}", async Task<IResult> (string token, ICompanyCheckManager manager) =>
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return Results.NotFound();
    }

    var request = await manager.GetRequestByDownloadTokenAsync(token);
    if (request is null || string.IsNullOrWhiteSpace(request.ReportPdfPath))
    {
        return Results.NotFound();
    }

    var storageRoot = manager.StorageRootPath;
    var baseDirectory = Path.GetFullPath(storageRoot);
    var physicalPath = Path.GetFullPath(Path.Combine(storageRoot, request.ReportPdfPath));

    if (!physicalPath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase) || !File.Exists(physicalPath))
    {
        return Results.NotFound();
    }

    var fileName = "Bonix_auskunft.pdf";
    return Results.File(physicalPath, "application/pdf", fileName);
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    // Add additional assemblies so RCL pages can be discovered for routing
    ;
app.Run();
