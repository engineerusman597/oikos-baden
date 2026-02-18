using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Application.Extensions;

namespace Oikos.Web.Components.Pages.User_Bonix.CompanyChecks;

public partial class History
{
    private List<CompanyCheckHistoryItem> _items = new();
    private bool _isLoading = true;

    [Inject] private ILogger<History> Logger { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.GetUserId();

            if (userId > 0)
            {
                var items = await _companyCheckManager.GetCompletedChecksAsync(userId);
                _items = items.ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load company check history for bonix user");
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
