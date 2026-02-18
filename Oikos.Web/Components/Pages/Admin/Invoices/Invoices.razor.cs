using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Domain.Enums;
using static Oikos.Web.Components.Shared.Pages.PagePagination;

namespace Oikos.Web.Components.Pages.Admin.Invoices;

public partial class Invoices
{
    [SupplyParameterFromQuery(Name = "stageId")]
    public int? StageIdQuery { get; set; }

    [SupplyParameterFromQuery(Name = "primaryStatus")]
    public string? PrimaryStatusQuery { get; set; }

    [Inject] private NavigationManager NavManager { get; set; } = null!;
    [Inject] private IWebHostEnvironment Env { get; set; } = null!;
    [Inject] private IInvoiceManagementService InvoiceService { get; set; } = null!;

    private readonly List<InvoiceListItemDto> _invoices = new();
    private List<InvoiceStageDto> _stageOptions = new();
    private SearchObject _searchObject = new();

    private bool _canRefresh = true;
    private bool _canChangeStatus = true;
    private bool _canViewDetails = true;
    private bool _filtersOpen;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();



        if (StageIdQuery.HasValue)
        {
            _searchObject.StageId = StageIdQuery.Value;
        }

        if (!string.IsNullOrWhiteSpace(PrimaryStatusQuery) && Enum.TryParse<InvoicePrimaryStatus>(PrimaryStatusQuery, true, out var status))
        {
            _searchObject.PrimaryStatus = status;
        }

        await LoadStageOptionsAsync();
        await LoadInvoicesAsync();
    }

    private async Task LoadStageOptionsAsync()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _stageOptions = await InvoiceService.GetInvoiceStagesAsync(culture);
    }

    private void NavigateToDetails(int invoiceId)
        => NavManager.NavigateTo($"/admin/invoices/{invoiceId}");

    private async Task LoadInvoicesAsync()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var request = new InvoiceSearchRequest(
            SearchText: _searchObject.SearchText,
            StageId: _searchObject.StageId,
            PrimaryStatus: _searchObject.PrimaryStatus,
            Page: _searchObject.Page,
            PageSize: _searchObject.Size);

        var result = await InvoiceService.SearchInvoicesAsync(request, culture);

        _searchObject.Total = result.TotalCount;

        // Adjust page if necessary
        if (_searchObject.Page > result.TotalPages)
        {
            _searchObject.Page = result.TotalPages;
        }

        _invoices.Clear();
        _invoices.AddRange(result.Items);
    }

    private async Task PageChangedClick(int page)
    {
        _searchObject.Page = page;
        await LoadInvoicesAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        _searchObject.Page = 1;
        await LoadInvoicesAsync();
    }

    private async Task RefreshAsync()
    {
        if (!_canRefresh)
        {
            return;
        }

        await LoadStageOptionsAsync();
        await LoadInvoicesAsync();
    }

    private void ToggleFilters() => _filtersOpen = !_filtersOpen;

    private async Task SearchReset()
    {
        _searchObject = new SearchObject();
        await LoadInvoicesAsync();
    }

    private async Task ChangeInvoiceStageAsync(InvoiceListItemDto invoice, int stageId)
    {
        if (!_canChangeStatus || invoice.StageId == stageId)
        {
            return;
        }

        var userId = await _authenticationService.GetUserIdAsync();
        var userName = await _authenticationService.GetUserNameAsync();

        var success = await InvoiceService.ChangeInvoiceStageAsync(invoice.Id, stageId, userId, userName);

        if (!success)
        {
            _snackbarService.Add(Loc["AdminInvoiceNotFound"], Severity.Error);
            await LoadInvoicesAsync();
            return;
        }

        // Update local model
        var stage = _stageOptions.FirstOrDefault(s => s.Id == stageId);
        if (stage != null)
        {
            var index = _invoices.IndexOf(invoice);
            if (index >= 0)
            {
                _invoices[index] = invoice with
                {
                    StageId = stageId,
                    StageName = stage.Name,
                    PrimaryStatus = stage.PrimaryStatus,
                    StageColor = stage.Color,
                    UpdatedAt = DateTime.Now
                };
            }
        }

        _snackbarService.Add(Loc["AdminStatusUpdated"], Severity.Success);
    }

    private async Task DeleteInvoiceAsync(InvoiceListItemDto invoice)
    {
        var confirmed = await _dialogService.ShowMessageBox(
            Loc["InvoiceDeleteTitle"],
            Loc["InvoiceDeleteMessage"],
            yesText: Loc["InvoiceDeleteConfirm"],
            cancelText: Loc["InvoiceDeleteCancel"]);

        if (confirmed != true)
        {
            return;
        }

        try
        {
            var storageRoot = GetStorageRoot();
            var success = await InvoiceService.DeleteInvoiceAsync(invoice.Id, storageRoot);

            if (!success)
            {
                _snackbarService.Add(Loc["InvoiceDeleteNotFound"], Severity.Warning);
                return;
            }

            _snackbarService.Add(Loc["InvoiceDeleteSuccess"], Severity.Success);
            await LoadInvoicesAsync();
        }
        catch
        {
            _snackbarService.Add(Loc["InvoiceDeleteError"], Severity.Error);
        }
    }

    private string FormatAmount(InvoiceListItemDto invoice)
    {
        var amount = string.IsNullOrWhiteSpace(invoice.Amount)
            ? Loc["TableValueUnknown"].ToString()
            : invoice.Amount!.Trim();

        if (string.IsNullOrWhiteSpace(invoice.Currency))
        {
            return amount;
        }

        return string.IsNullOrWhiteSpace(invoice.Amount)
            ? invoice.Currency!
            : $"{amount} {invoice.Currency}";
    }

    private string FormatCreatedAt(InvoiceListItemDto invoice)
        => invoice.CreatedAt.ToLocalTime().ToString("d", CultureInfo.CurrentUICulture);

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

    private string GetStorageRoot()
    {
        if (!string.IsNullOrWhiteSpace(Env.WebRootPath))
        {
            return Env.WebRootPath;
        }

        if (!string.IsNullOrWhiteSpace(Env.ContentRootPath))
        {
            return Path.Combine(Env.ContentRootPath, "wwwroot");
        }

        return Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }

    private record SearchObject : PaginationModel
    {
        public string? SearchText { get; set; }
        public int? StageId { get; set; }
        public InvoicePrimaryStatus? PrimaryStatus { get; set; }
    }
}
