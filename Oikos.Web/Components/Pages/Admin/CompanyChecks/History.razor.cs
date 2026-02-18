using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Web.Components.Pages.Admin.CompanyChecks;

public partial class History
{
    private List<CompanyCheckHistoryItem> _items = new();
    private bool _isLoading = true;

    [Inject] private ILogger<History> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Admin: Get all completed checks, not filtered by user
            var items = await _companyCheckManager.GetAllCompletedChecksAsync();
            _items = items.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load company check history for admin");
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private string FormatPaidAt(DateTime? paidAt)
    {
        return paidAt?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "-";
    }

    private async Task ShowDetailsAsync(CompanyCheckHistoryItem item)
    {
        var parameters = new DialogParameters<Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs.CompanyCheckDetailDialog>
        {
            { nameof(Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs.CompanyCheckDetailDialog.Item), item }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        await _dialogService.ShowAsync<Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs.CompanyCheckDetailDialog>(Loc["DetailsDialogTitle"], parameters, options);
    }
}
