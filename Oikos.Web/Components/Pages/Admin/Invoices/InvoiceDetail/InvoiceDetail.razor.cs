using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Domain.Enums;
using Oikos.Common.Resources;
using Oikos.Web.Components.Pages.Admin.Invoices.Dialogs;

namespace Oikos.Web.Components.Pages.Admin.Invoices.InvoiceDetail;

public partial class InvoiceDetail
{
    [Parameter] public int InvoiceId { get; set; }

    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private IInvoiceManagementService InvoiceService { get; set; } = null!;
    [Inject] private Oikos.Application.Services.Authentication.IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private InvoiceDetailDto? _invoice;
    private List<InvoiceHistoryDto> _history = new();
    private List<InvoiceHistoryDto> _notes = new();
    private int _noteCount => _notes.Count;
    private List<InvoiceStageDto> _allStages = new();
    private bool _isLoading = true;
    private bool _isSaving = false;

    // Statuses that allow transitioning to Inquiry (matches StatusChangeDialog.ValidTransitions)
    private static readonly HashSet<InvoicePrimaryStatus> StatusesAllowingInquiry = new()
    {
        InvoicePrimaryStatus.Submitted,
        InvoicePrimaryStatus.InReview,
    };

    private bool _canSendRueckfrage =>
        _invoice is not null &&
        StatusesAllowingInquiry.Contains(_invoice.PrimaryStatus) &&
        _allStages.Any(s => s.PrimaryStatus == InvoicePrimaryStatus.Inquiry);

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
        _notes = _history.Where(h => !string.IsNullOrWhiteSpace(h.Note)).ToList();
    }

    private async Task OpenStatusDialogAsync()
    {
        var parameters = new DialogParameters
        {
            { nameof(StatusChangeDialog.Stages), _allStages },
            { nameof(StatusChangeDialog.CurrentStageId), _invoice?.StageId },
            { nameof(StatusChangeDialog.CurrentPrimaryStatus), _invoice!.PrimaryStatus },
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<StatusChangeDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: StatusChangeDialog.StatusChangeDialogResult changeResult })
        {
            await SaveStatusUpdateAsync(changeResult.StageId, changeResult.Note);
        }
    }

    private async Task OpenRueckfrageDialogAsync()
    {
        var inquiryStages = _allStages
            .Where(s => s.PrimaryStatus == InvoicePrimaryStatus.Inquiry)
            .ToList();

        var parameters = new DialogParameters
        {
            { nameof(RueckfrageDialog.InquiryStages), inquiryStages },
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<RueckfrageDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: StatusChangeDialog.StatusChangeDialogResult changeResult })
        {
            await SaveStatusUpdateAsync(changeResult.StageId, changeResult.Note);
        }
    }

    private async Task SaveStatusUpdateAsync(int stageId, string? note = null)
    {
        if (_invoice == null) return;

        _isSaving = true;
        StateHasChanged();

        var userId = await AuthenticationService.GetUserIdAsync();
        var userName = await AuthenticationService.GetUserNameAsync();

        var success = await InvoiceService.ChangeInvoiceStageAsync(InvoiceId, stageId, userId, userName, note);

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
