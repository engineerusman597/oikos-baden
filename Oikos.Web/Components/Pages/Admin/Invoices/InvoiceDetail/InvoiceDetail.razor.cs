using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Domain.Enums;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.Invoices.InvoiceDetail;

public partial class InvoiceDetail
{
    [Parameter] public int InvoiceId { get; set; }

    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private IInvoiceManagementService InvoiceService { get; set; } = null!;
    [Inject] private Oikos.Application.Services.Authentication.IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;

    private InvoiceDetailDto? _invoice;
    private List<InvoiceHistoryDto> _history = new();
    private List<InvoiceStageDto> _allStages = new();
    private List<InvoiceStageDto> _availableStages = new();
    private bool _isLoading = true;
    private bool _isSaving = false;

    private InvoicePrimaryStatus? _selectedPrimaryStatus;
    private int? _selectedStageId;

    private bool _canSave => _selectedStageId.HasValue && _selectedStageId != _invoice?.StageId;

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        await LoadInvoiceAsync();
        _isLoading = false;
    }

    private async Task LoadInvoiceAsync()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _invoice = await InvoiceService.GetInvoiceDetailAsync(InvoiceId, culture);
        _allStages = await InvoiceService.GetInvoiceStagesAsync(culture);
        
        _history = _invoice?.History ?? new List<InvoiceHistoryDto>();

        if (_invoice != null)
        {
            _selectedPrimaryStatus = _invoice.PrimaryStatus;
            _selectedStageId = _invoice.StageId;
            FilterStages();
        }
    }

    private void OnPrimaryStatusChanged(InvoicePrimaryStatus? status)
    {
        _selectedPrimaryStatus = status;
        _selectedStageId = null;
        FilterStages();
    }

    private void FilterStages()
    {
        if (_selectedPrimaryStatus.HasValue)
        {
            _availableStages = _allStages.Where(s => s.PrimaryStatus == _selectedPrimaryStatus.Value).ToList();
        }
        else
        {
            _availableStages = new();
        }
    }

    private async Task SaveStatusUpdateAsync()
    {
        if (!_selectedStageId.HasValue || _invoice == null) return;

        _isSaving = true;
        
        var userId = await AuthenticationService.GetUserIdAsync();
        var userName = await AuthenticationService.GetUserNameAsync();

        var success = await InvoiceService.ChangeInvoiceStageAsync(InvoiceId, _selectedStageId.Value, userId, userName);

        if (success)
        {
            SnackbarService.Add(Loc["InvoiceDetail_StatusUpdateSuccess"], Severity.Success);
            await LoadInvoiceAsync();
        }
        else
        {
            SnackbarService.Add(Loc["InvoiceDetail_StatusUpdateError"], Severity.Error);
        }

        _isSaving = false;
    }

    private void GoBack() => NavManager.NavigateTo("/admin/invoices");

    private Color ResolveColor(string? colorName, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(colorName) && Enum.TryParse<Color>(colorName, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private string FormatCompany(InvoiceDetailDto invoice)
        => string.IsNullOrWhiteSpace(invoice.Company) ? Loc["TableValueUnknown"] : invoice.Company!;

    private Color GetPrimaryStatusColor(InvoicePrimaryStatus status)
    {
        return status switch
        {
            InvoicePrimaryStatus.Draft => Color.Default,
            InvoicePrimaryStatus.Submitted => Color.Info,
            InvoicePrimaryStatus.InReview => Color.Warning,
            InvoicePrimaryStatus.Inquiry => Color.Error,
            InvoicePrimaryStatus.Accepted => Color.Success,
            InvoicePrimaryStatus.Court => Color.Secondary,
            InvoicePrimaryStatus.Completed => Color.Dark,
            _ => Color.Default
        };
    }

    private string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? Loc["TableValueUnknown"] : value!;

    private string FormatAmount(InvoiceDetailDto invoice)
    {
        var amount = string.IsNullOrWhiteSpace(invoice.Amount) ? Loc["TableValueUnknown"] : invoice.Amount!.Trim();
        if (string.IsNullOrWhiteSpace(invoice.Currency))
        {
            return amount;
        }

        return string.IsNullOrWhiteSpace(invoice.Amount)
            ? invoice.Currency!
            : $"{amount} {invoice.Currency}";
    }

    private string FormatDate(DateTime? date)
        => date.HasValue
            ? date.Value.ToLocalTime().ToString("d", CultureInfo.CurrentUICulture)
            : Loc["TableValueUnknown"];

    private string FormatDateTime(DateTime date)
        => date.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);

    private static string GetFileUrl(string path)
        => $"/{path.TrimStart('/')}";
}
