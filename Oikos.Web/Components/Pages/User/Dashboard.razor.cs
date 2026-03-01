using System.Globalization;
using Microsoft.AspNetCore.Components;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Application.Services.Dashboard;
using Oikos.Application.Services.Dashboard.Models;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Application.Services.Setting;
using MudBlazor;
using Oikos.Domain.Enums;
using Oikos.Web.Constants;
using Oikos.Common.Helpers;
using Oikos.Web.Components.Pages.User.Models;

namespace Oikos.Web.Components.Pages.User;

public partial class Dashboard
{
    private static string PricingUrl => ExternalUrlConstants.PricingUrl;
    private string _greetingText = string.Empty;

    private readonly List<StatusSummary> _statusSummaries = new();

    private DashboardNewsContent? _news;

    private bool _subscriptionChecked;
    private bool _hasActiveSubscription;
    private int _totalClaims;
    private int _remainingClaims;
    private string _planName = string.Empty;

    private List<MyInvoiceItemDto> _recentInvoices = new();
    private List<DashboardRecentActivityDto> _recentActivities = new();

    [Inject]
    private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    [Inject]
    private IDashboardService DashboardService { get; set; } = null!;

    [Inject]
    private IInvoiceManagementService InvoiceManagementService { get; set; } = null!;

    [Inject]
    private ISettingService SettingService { get; set; } = null!;

    private static readonly IReadOnlyDictionary<string, string> StageColorMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Primary"] = "var(--mud-palette-primary)",
            ["Secondary"] = "var(--mud-palette-secondary)",
            ["Tertiary"] = "var(--mud-palette-secondary)",
            ["Success"] = "var(--mud-palette-success)",
            ["Warning"] = "var(--mud-palette-warning)",
            ["Error"] = "var(--mud-palette-error)",
            ["Info"] = "var(--mud-palette-info)",
            ["Dark"] = "var(--mud-palette-dark)",
            ["Background"] = "var(--mud-palette-background)",
            ["Surface"] = "var(--mud-palette-surface)",
            ["Light"] = "var(--mud-palette-surface)",
            ["Default"] = "var(--mud-palette-primary)",
            ["Inherit"] = "var(--mud-palette-text-primary)"
        };

    private static readonly IReadOnlyDictionary<string, string> StageColorRgbMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Primary"] = "var(--mud-palette-primary-rgb)",
            ["Secondary"] = "var(--mud-palette-secondary-rgb)",
            ["Tertiary"] = "var(--mud-palette-secondary-rgb)",
            ["Success"] = "var(--mud-palette-success-rgb)",
            ["Warning"] = "var(--mud-palette-warning-rgb)",
            ["Error"] = "var(--mud-palette-error-rgb)",
            ["Info"] = "var(--mud-palette-info-rgb)",
            ["Dark"] = "var(--mud-palette-dark-rgb)",
            ["Background"] = "var(--mud-palette-background-rgb)",
            ["Surface"] = "var(--mud-palette-surface-rgb)",
            ["Light"] = "var(--mud-palette-surface-rgb)",
            ["Default"] = "var(--mud-palette-primary-rgb)",
            ["Inherit"] = "var(--mud-palette-text-primary-rgb)"
        };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var userInfo = await _authenticationService.GetUserInfoAsync();

        if (userInfo != null)
        {
            await LoadDashboardDataAsync(userInfo.Id, false);
            await LoadNewsAsync();
            await LoadRecentDataAsync(userInfo.Id);
            _greetingText = GreetingHelper.BuildGreeting(userInfo.RealName, Loc);
            await EvaluateSubscriptionAsync(userInfo.Id);
        }
        else
        {
            // Fallback for unauthenticated access if needed (usually protected by routing)
            await LoadDashboardDataAsync(0, false);
            await LoadNewsAsync();
            _greetingText = GreetingHelper.BuildGreeting(null, Loc);
        }
    }

    private async Task LoadNewsAsync()
    {
        var settings = await SettingService.GetDashboardNewsSettingsAsync();
        
        if (string.IsNullOrWhiteSpace(settings.Title) && string.IsNullOrWhiteSpace(settings.Summary) && string.IsNullOrWhiteSpace(settings.Link))
        {
            _news = null;
            return;
        }

        _news = new DashboardNewsContent(settings.Title, settings.Summary, settings.Link);
    }

    private async Task LoadDashboardDataAsync(int userId, bool isAdminDashboard)
    {
        var summaries = await DashboardService.GetDashboardStatusSummariesAsync(userId, isAdminDashboard);

        _statusSummaries.Clear();

        // 1. Eingereicht (Submitted)
        var submittedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Submitted)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Neu"], submittedCount, Icons.Material.Filled.Description, "/invoices?primaryStatus=Submitted", "Primary");

        // 2. In Prüfung (InReview)
        var inReviewCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.InReview)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_InPruefung"], inReviewCount, Icons.Material.Filled.AccessTime, "/invoices?primaryStatus=InReview", "Warning");

        // 3. Rückfragen (Inquiry)
        var inquiryCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Inquiry)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Rueckfragen"], inquiryCount, Icons.Material.Filled.ErrorOutline, "/invoices?primaryStatus=Inquiry", "Error");

        // 4. Akzeptiert (Accepted)
        var acceptedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Accepted)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Akzeptiert"], acceptedCount, Icons.Material.Filled.CheckCircleOutline, "/invoices?primaryStatus=Accepted", "Success");

        // 5. Gericht (Court)
        var courtCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Court)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Gericht"], courtCount, Icons.Material.Filled.Gavel, "/invoices?primaryStatus=Court", "Error");

        // 6. Fristen (DeadlineRunning)
        var fristenCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.DeadlineRunning)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Fristen"], fristenCount, Icons.Material.Filled.Timer, "/invoices?primaryStatus=DeadlineRunning", "Warning");

        // 7. Abgeschlossen (Completed)
        var completedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Completed)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Completed"], completedCount, Icons.Material.Filled.CheckCircle, "/invoices?primaryStatus=Completed", "Success");
    }

    private async Task LoadRecentDataAsync(int userId)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var myInvoices = await InvoiceManagementService.GetMyInvoicesAsync(userId, culture);
        _recentInvoices = myInvoices.Invoices
            .OrderByDescending(i => i.UpdatedAt)
            .Take(5)
            .ToList();

        _recentActivities = await DashboardService.GetRecentActivitiesAsync(userId, 5);
    }

    private void AddSummary(string name, int count, string icon, string targetUri, string color)
    {
        var style = BuildStageStyle(color);
        var cssClass = $"status-card--{name.Replace(' ', '-').ToLowerInvariant()}";

        _statusSummaries.Add(new StatusSummary(0, name, string.Empty, icon, targetUri, cssClass, style)
        {
            Count = count
        });
    }
    private void NavigateTo(string uri)
    {
        if (RequiresSubscription(uri) && !_hasActiveSubscription)
        {
            ShowMissingSubscriptionNotice();
            return;
        }

        _navManager.NavigateTo(uri);
    }

    private void NavigateToNewClaim() => NavigateTo("/cases/new");

    private void ShowMissingSubscriptionNotice()
    {
        _snackbarService.Add(Loc["SubscriptionRequiredMessage"], MudBlazor.Severity.Warning);
    }

    private void HandleNewClaimKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or " ")
        {
            NavigateToNewClaim();
        }
    }


    private string? LocalizeStageValue(string? english, string? german)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return culture switch
        {
            "de" => FirstNonEmpty(german, english),
            _ => FirstNonEmpty(english, german)
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string BuildStageStyle(string? colorValue)
    {
        var color = ResolveStageColor(colorValue);
        var colorRgb = ResolveStageColorRgb(colorValue);
        return $"--status-color: {color}; --status-color-rgb: {colorRgb};";
    }

    private static string ResolveStageColor(string? colorValue)
    {
        if (string.IsNullOrWhiteSpace(colorValue))
        {
            return "var(--mud-palette-info)";
        }

        if (StageColorMap.TryGetValue(colorValue, out var mapped))
        {
            return mapped;
        }

        return "var(--mud-palette-info)";
    }

    private static string ResolveStageColorRgb(string? colorValue)
    {
        if (string.IsNullOrWhiteSpace(colorValue))
        {
            return "var(--mud-palette-info-rgb)";
        }

        if (StageColorRgbMap.TryGetValue(colorValue, out var mapped))
        {
            return mapped;
        }

        var trimmed = colorValue.Trim();
        if (TryParseHexColor(trimmed, out var rgb))
        {
            return $"{rgb.R}, {rgb.G}, {rgb.B}";
        }

        return "var(--mud-palette-info-rgb)";
    }

    private static bool TryParseHexColor(string value, out (int R, int G, int B) rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(value) || value[0] != '#')
        {
            return false;
        }

        var hex = value[1..];
        if (hex.Length == 3)
        {
            if (!TryParseHexNibble(hex[0], out var r)
                || !TryParseHexNibble(hex[1], out var g)
                || !TryParseHexNibble(hex[2], out var b))
            {
                return false;
            }

            rgb = (r * 17, g * 17, b * 17);
            return true;
        }

        if (hex.Length == 6)
        {
            if (!TryParseHexByte(hex[..2], out var r)
                || !TryParseHexByte(hex[2..4], out var g)
                || !TryParseHexByte(hex[4..6], out var b))
            {
                return false;
            }

            rgb = (r, g, b);
            return true;
        }

        return false;
    }

    private static bool TryParseHexNibble(char value, out int result)
    {
        result = 0;
        if (value is >= '0' and <= '9')
        {
            result = value - '0';
            return true;
        }

        if (value is >= 'a' and <= 'f')
        {
            result = value - 'a' + 10;
            return true;
        }

        if (value is >= 'A' and <= 'F')
        {
            result = value - 'A' + 10;
            return true;
        }

        return false;
    }

    private static bool TryParseHexByte(string value, out int result)
    {
        result = 0;
        if (value.Length != 2)
        {
            return false;
        }

        return TryParseHexNibble(value[0], out var high)
            && TryParseHexNibble(value[1], out var low)
            && (result = (high * 16) + low) >= 0;
    }

    private async Task EvaluateSubscriptionAsync(int userId)
    {
        if (await _authenticationService.IsAdminAsync())
        {
            _hasActiveSubscription = true;
            _subscriptionChecked = true;
            _planName = "Administrator";
            _totalClaims = -1;
            return;
        }

        var subscription = await SubscriptionPlanService.GetActiveSubscriptionAsync(userId);

        if (subscription == null)
        {
            _hasActiveSubscription = false;
            _planName = Loc["Dashboard_NoPlan"];
            _totalClaims = 0;
            _remainingClaims = 0;
            _subscriptionChecked = true;
            return;
        }

        _hasActiveSubscription = IsActiveSubscription(subscription);
        _planName = subscription.PlanName;
        _totalClaims = subscription.MonthlyClaimLimit ?? 0;
        
        var check = await SubscriptionPlanService.CheckClaimSubmissionAsync(userId, 0);
        _remainingClaims = check.Remaining ?? 0;
        
        _subscriptionChecked = true;
    }

    private static bool IsActiveSubscription(UserSubscriptionSnapshot? subscription)
    {
        if (subscription == null)
        {
            return false;
        }

        return subscription.UserSubscriptionId.HasValue
            && (!subscription.ExpirationDate.HasValue || subscription.ExpirationDate.Value > DateTime.UtcNow);
    }

    private string GetStatusLabel(InvoicePrimaryStatus status) => status switch
    {
        InvoicePrimaryStatus.Submitted       => Loc["AdminDashboard_Status_Neu"],
        InvoicePrimaryStatus.InReview        => Loc["AdminDashboard_Status_InPruefung"],
        InvoicePrimaryStatus.Inquiry         => Loc["AdminDashboard_Status_Rueckfragen"],
        InvoicePrimaryStatus.Accepted        => Loc["AdminDashboard_Status_Akzeptiert"],
        InvoicePrimaryStatus.CourtPrep       => Loc["AdminDashboard_Status_Akzeptiert"],
        InvoicePrimaryStatus.Court           => Loc["AdminDashboard_Status_Gericht"],
        InvoicePrimaryStatus.DeadlineRunning => Loc["AdminDashboard_Status_Fristen"],
        InvoicePrimaryStatus.Completed       => Loc["AdminDashboard_Status_Completed"],
        InvoicePrimaryStatus.Cancelled       => Loc["Dashboard_Status_Cancelled"],
        InvoicePrimaryStatus.Rejected        => Loc["Dashboard_Status_Rejected"],
        _                                    => status.ToString()
    };

    private static Color GetStatusColor(InvoicePrimaryStatus status) => status switch
    {
        InvoicePrimaryStatus.Submitted       => Color.Primary,
        InvoicePrimaryStatus.InReview        => Color.Warning,
        InvoicePrimaryStatus.Inquiry         => Color.Error,
        InvoicePrimaryStatus.Accepted        => Color.Success,
        InvoicePrimaryStatus.CourtPrep       => Color.Success,
        InvoicePrimaryStatus.Court           => Color.Error,
        InvoicePrimaryStatus.DeadlineRunning => Color.Warning,
        InvoicePrimaryStatus.Completed       => Color.Success,
        _                                    => Color.Default
    };

    private static bool RequiresSubscription(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        return uri.StartsWith("/cases/new", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("/company-checks", StringComparison.OrdinalIgnoreCase);
    }


}
