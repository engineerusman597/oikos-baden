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
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;

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

    [Inject]
    private IInvoiceManagementService InvoiceManagementService { get; set; } = null!;

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

        await LoadTabInvoicesAsync();
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

        int Count(params InvoicePrimaryStatus[] statuses) =>
            statuses.Sum(s => summaries.FirstOrDefault(x => x.PrimaryStatus == s)?.Count ?? 0);

        // 1. Neu
        AddSummary(Loc["AdminDashboard_Status_Neu"],          Count(InvoicePrimaryStatus.Submitted),
            Icons.Material.Filled.Description,       "/admin/invoices?primaryStatus=Submitted", "Primary");

        // 2. In Prüfung
        AddSummary(Loc["AdminDashboard_Status_InPruefung"],   Count(InvoicePrimaryStatus.InReview),
            Icons.Material.Filled.AccessTime,         "/admin/invoices?primaryStatus=InReview",  "Warning");

        // 3. Rückfragen
        AddSummary(Loc["AdminDashboard_Status_Rueckfragen"],  Count(InvoicePrimaryStatus.Inquiry),
            Icons.Material.Filled.ErrorOutline,       "/admin/invoices?primaryStatus=Inquiry",   "Error");

        // 4. Akzeptiert (Accepted + CourtPrep)
        AddSummary(Loc["AdminDashboard_Status_Akzeptiert"],   Count(InvoicePrimaryStatus.Accepted, InvoicePrimaryStatus.CourtPrep),
            Icons.Material.Filled.CheckCircleOutline, "/admin/invoices?primaryStatus=Accepted",  "Success");

        // 5. Gericht (Court + WaitingCourt + CourtResponse)
        AddSummary(Loc["AdminDashboard_Status_Gericht"],      Count(InvoicePrimaryStatus.Court, InvoicePrimaryStatus.WaitingCourt, InvoicePrimaryStatus.CourtResponse),
            Icons.Material.Filled.Gavel,              "/admin/invoices?primaryStatus=Court",     "Secondary");

        // 6. Fristen (DeadlineRunning)
        AddSummary(Loc["AdminDashboard_Status_Fristen"],      Count(InvoicePrimaryStatus.DeadlineRunning),
            Icons.Material.Filled.Timer,              "/admin/invoices?primaryStatus=DeadlineRunning", "Error");

        // 7. Vollstreckung (EnforcementReady + EnforcementInProgress)
        AddSummary(Loc["AdminDashboard_Status_Vollstreckung"], Count(InvoicePrimaryStatus.EnforcementReady, InvoicePrimaryStatus.EnforcementInProgress),
            Icons.Material.Filled.Scale,              "/admin/invoices?primaryStatus=EnforcementReady", "Success");
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

    // ── Queue / tab state ─────────────────────────────────────────────────────

    private MudTabs? _tabs;
    private int _activeTabIndex;
    private bool _loadingInvoices;
    private readonly List<InvoiceListItemDto> _tabInvoices = new();
    private int _tabTotalCount;
    private const int TabPageSize = 20;

    private static readonly IReadOnlyList<QueueDefinition> Queues = new List<QueueDefinition>
    {
        new("AdminDashboard_Status_Neu",          Icons.Material.Filled.Description,
            new[] { InvoicePrimaryStatus.Submitted }),

        new("AdminDashboard_Status_InPruefung",   Icons.Material.Filled.AccessTime,
            new[] { InvoicePrimaryStatus.InReview }),

        new("AdminDashboard_Status_Rueckfragen",  Icons.Material.Filled.ErrorOutline,
            new[] { InvoicePrimaryStatus.Inquiry }),

        new("AdminDashboard_Status_Akzeptiert",   Icons.Material.Filled.CheckCircleOutline,
            new[] { InvoicePrimaryStatus.Accepted, InvoicePrimaryStatus.CourtPrep }),

        new("AdminDashboard_Status_Gericht",      Icons.Material.Filled.Gavel,
            new[] { InvoicePrimaryStatus.Court, InvoicePrimaryStatus.WaitingCourt, InvoicePrimaryStatus.CourtResponse }),

        new("AdminDashboard_Status_Fristen",      Icons.Material.Filled.Timer,
            new[] { InvoicePrimaryStatus.DeadlineRunning }),

        new("AdminDashboard_Status_Vollstreckung",Icons.Material.Filled.Scale,
            new[] { InvoicePrimaryStatus.EnforcementReady, InvoicePrimaryStatus.EnforcementInProgress }),
    };

    private void SelectTab(int index) => _tabs?.ActivatePanel(index);

    private async Task OnTabChangedAsync(int index)
    {
        _activeTabIndex = index;
        await LoadTabInvoicesAsync();
    }

    private async Task LoadTabInvoicesAsync()
    {
        _loadingInvoices = true;
        StateHasChanged();

        var queue = Queues[_activeTabIndex];
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var request = new InvoiceSearchRequest(
            SearchText: null,
            StageId: null,
            PrimaryStatus: null,
            Page: 1,
            PageSize: TabPageSize,
            PrimaryStatuses: queue.Statuses);

        var result = await InvoiceManagementService.SearchInvoicesAsync(request, culture);
        _tabTotalCount = result.TotalCount;
        _tabInvoices.Clear();
        _tabInvoices.AddRange(result.Items);

        _loadingInvoices = false;
        StateHasChanged();
    }

    private string FormatAmount(InvoiceListItemDto invoice)
    {
        var amount = string.IsNullOrWhiteSpace(invoice.Amount)
            ? Loc["TableValueUnknown"].ToString()
            : invoice.Amount.Trim();

        if (string.IsNullOrWhiteSpace(invoice.Currency)) return amount;

        return string.IsNullOrWhiteSpace(invoice.Amount)
            ? invoice.Currency
            : $"{amount} {invoice.Currency}";
    }

    private string FormatCreatedAt(InvoiceListItemDto invoice)
        => invoice.CreatedAt.ToLocalTime().ToString("d", CultureInfo.CurrentUICulture);

    private static Color GetPrimaryStatusColor(InvoicePrimaryStatus status) =>
        status switch
        {
            InvoicePrimaryStatus.Draft                  => Color.Default,
            InvoicePrimaryStatus.Submitted              => Color.Info,
            InvoicePrimaryStatus.InReview               => Color.Warning,
            InvoicePrimaryStatus.Inquiry                => Color.Error,
            InvoicePrimaryStatus.Accepted               => Color.Success,
            InvoicePrimaryStatus.CourtPrep              => Color.Secondary,
            InvoicePrimaryStatus.Court                  => Color.Secondary,
            InvoicePrimaryStatus.WaitingCourt           => Color.Secondary,
            InvoicePrimaryStatus.DeadlineRunning        => Color.Error,
            InvoicePrimaryStatus.CourtResponse          => Color.Info,
            InvoicePrimaryStatus.EnforcementReady       => Color.Success,
            InvoicePrimaryStatus.EnforcementInProgress  => Color.Success,
            InvoicePrimaryStatus.Completed              => Color.Dark,
            InvoicePrimaryStatus.Cancelled              => Color.Default,
            InvoicePrimaryStatus.Rejected               => Color.Error,
            _                                           => Color.Default
        };

    private sealed record QueueDefinition(string LocalizationKey, string Icon, IReadOnlyList<InvoicePrimaryStatus> Statuses)
    {
        public string ViewAllUri => Statuses.Count == 1
            ? $"/admin/invoices?primaryStatus={Statuses[0]}"
            : "/admin/invoices";
    }

    // ── Existing inner types ───────────────────────────────────────────────────

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
