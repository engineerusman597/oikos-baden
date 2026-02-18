using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Domain.Enums;
using Oikos.Web.Components.Invoice;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Web.Components.Pages.User.Invoices;

public partial class Invoices
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IInvoiceManagementService InvoiceService { get; set; } = null!;
    //[Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!; // Not used for now if quota is removed

    private List<MyInvoiceItemDto> _allInvoices = new();
    private List<MyInvoiceItemDto> _filteredInvoices = new();
    private List<InvoiceStageWithCountDto> _stages = new();
    private Dictionary<int, InvoiceStageWithCountDto> _stageById = new();

    private string _searchString = string.Empty;
    private InvoicePrimaryStatus? _selectedPrimaryStatus;

    [SupplyParameterFromQuery] public string? PrimaryStatus { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var userId = await _authenticationService.GetUserIdAsync();
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        // Load invoices and stages
        var data = await InvoiceService.GetMyInvoicesAsync(userId, culture);
        
        // Populate stages
        _stages = data.Stages;
        _stageById = _stages.ToDictionary(s => s.Id);

        // Populate invoices
        _allInvoices = data.Invoices;
        
        // Handle Query Parameter
        if (!string.IsNullOrWhiteSpace(PrimaryStatus) && 
            Enum.TryParse<InvoicePrimaryStatus>(PrimaryStatus, true, out var parsedStatus))
        {
            _selectedPrimaryStatus = parsedStatus;
        }

        // Initial filter
        FilterInvoices();
    }

    private void OnSearch(string text)
    {
        _searchString = text;
        FilterInvoices();
    }

    private void OnStatusFilterChanged(InvoicePrimaryStatus? status)
    {
        _selectedPrimaryStatus = status;
        FilterInvoices();
    }

    private void FilterInvoices()
    {
        var result = _allInvoices.AsEnumerable();

        // 1. Filter by Status
        if (_selectedPrimaryStatus.HasValue)
        {
            result = result.Where(i => i.PrimaryStatus == _selectedPrimaryStatus.Value);
        }

        // 2. Filter by Search Text
        if (!string.IsNullOrWhiteSpace(_searchString))
        {
            var search = _searchString.Trim();
            result = result.Where(i => 
                (i.TicketNumber != null && i.TicketNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (i.Company != null && i.Company.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (i.Amount != null && i.Amount.Contains(search, StringComparison.OrdinalIgnoreCase))
            );
        }

        _filteredInvoices = result.OrderByDescending(i => i.UpdatedAt).ToList();
        StateHasChanged();
    }

    private Color GetStageColor(int stageId)
    {
        if (_stageById.TryGetValue(stageId, out var stage) &&
            !string.IsNullOrWhiteSpace(stage.Color) &&
            Enum.TryParse<Color>(stage.Color, out var parsed))
        {
            return parsed;
        }
        return Color.Info;
    }

    private string GetStageName(int stageId)
        => _stageById.TryGetValue(stageId, out var stage) ? stage.Name : Loc["StageUnknown"];

    private string FormatCompany(MyInvoiceItemDto invoice)
        => string.IsNullOrWhiteSpace(invoice.Company) ? Loc["TableValueUnknown"] : invoice.Company!;

    private string FormatAmount(MyInvoiceItemDto invoice)
        => string.IsNullOrWhiteSpace(invoice.Amount) ? Loc["TableValueUnknown"] : invoice.Amount!.Trim();

    private string FormatCurrency(MyInvoiceItemDto invoice)
        => string.IsNullOrWhiteSpace(invoice.Currency) ? Loc["TableValueUnknown"] : invoice.Currency!;

    private string FormatDate(DateTime? date)
        => date.HasValue ? date.Value.ToString("d", CultureInfo.CurrentUICulture) : Loc["TableValueUnknown"];

    private string FormatDateTime(DateTime date)
        => date.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);

    private void NavigateToInvoiceDetail(MyInvoiceItemDto invoice)
        => Navigation.NavigateTo($"/invoices/{invoice.Id}?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
}
