using System.Globalization;
using Microsoft.AspNetCore.Components;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Application.Services.Dashboard;
using Oikos.Application.Services.Setting;
using MudBlazor;
using Oikos.Domain.Enums;
using Oikos.Application.Services.Dashboard.Models;
using Oikos.Web.Constants;
using Oikos.Common.Helpers;

namespace Oikos.Web.Components.Pages.Admin;

public partial class Dashboard
{
    private static string PricingUrl => ExternalUrlConstants.PricingUrl;
    private string _greetingText = string.Empty;

    private readonly List<StatusSummary> _statusSummaries = new();

    private DashboardNewsContent? _news;

    private bool _subscriptionChecked;
    private bool _hasActiveSubscription;

    [Inject]
    private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;
    
    [Inject]
    private IDashboardService DashboardService { get; set; } = null!;
    
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
            await LoadDashboardDataAsync(userInfo.Id, true);
            await LoadNewsAsync();
            _greetingText = GreetingHelper.BuildGreeting(userInfo.RealName, Loc);
            await EvaluateSubscriptionAsync(userInfo.Id);
        }
        else
        {
            // Defensive fallback
             await LoadDashboardDataAsync(0, true);
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

        // 1. Neu (Draft + Submitted)
        var draftCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Draft)?.Count ?? 0;
        var submittedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Submitted)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Neu"], draftCount + submittedCount, Icons.Material.Filled.Description, "/admin/invoices?primaryStatus=Submitted", "Primary");

        // 2. In Prüfung (InReview)
        var inReviewCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.InReview)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_InPruefung"], inReviewCount, Icons.Material.Filled.AccessTime, "/admin/invoices?primaryStatus=InReview", "Warning");

        // 3. Rückfragen (Inquiry)
        var inquiryCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Inquiry)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Rueckfragen"], inquiryCount, Icons.Material.Filled.ErrorOutline, "/admin/invoices?primaryStatus=Inquiry", "Error");

        // 4. Akzeptiert (Accepted)
        var acceptedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Accepted)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Akzeptiert"], acceptedCount, Icons.Material.Filled.CheckCircleOutline, "/admin/invoices?primaryStatus=Accepted", "Success");

        // 5. Gericht (Court)
        var courtCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Court)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Gericht"], courtCount, Icons.Material.Filled.Gavel, "/admin/invoices?primaryStatus=Court", "Error");

        // 6. Abgeschlossen (Completed)
        var completedCount = summaries.FirstOrDefault(s => s.PrimaryStatus == InvoicePrimaryStatus.Completed)?.Count ?? 0;
        AddSummary(Loc["AdminDashboard_Status_Completed"], completedCount, Icons.Material.Filled.CheckCircle, "/admin/invoices?primaryStatus=Completed", "Success");
    }

    private void AddSummary(string label, int count, string icon, string targetUri, string color)
    {
        var style = BuildStageStyle(color);
        _statusSummaries.Add(new StatusSummary(0, label, "", icon, targetUri, "", style)
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

    private void NavigateToNewClaim() => NavigateTo("/invoice/new");

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
            return;
        }

        var subscription = await SubscriptionPlanService.GetActiveSubscriptionAsync(userId);
        _hasActiveSubscription = IsActiveSubscription(subscription);
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

    private static bool RequiresSubscription(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        return uri.StartsWith("/invoice/new", StringComparison.OrdinalIgnoreCase)
            || uri.StartsWith("/company-checks", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StatusSummary(
        int StageId,
        string Name,
        string Description,
        string Icon,
        string TargetUri,
        string CssClass,
        string Style)
    {
        public int StageId { get; } = StageId;
        public string Name { get; } = Name;
        public string Description { get; } = Description;
        public string Icon { get; } = Icon;
        public string TargetUri { get; } = TargetUri;
        public string CssClass { get; } = CssClass;
        public string Style { get; } = Style;
        public int Count { get; set; }
    }

    private sealed record InvoiceStageCount(int Key, int Count);

    private sealed class DashboardNewsContent(string? Title, string? Summary, string? Link)
    {
        public string? Title { get; } = Title;

        public string? Summary { get; } = Summary;

        public string? Link { get; } = Link;

        public bool HasLink => !string.IsNullOrWhiteSpace(Link);
    }
}
