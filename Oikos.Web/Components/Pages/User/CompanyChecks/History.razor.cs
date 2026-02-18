using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Application.Services.Subscription;
using Oikos.Web.Constants;

namespace Oikos.Web.Components.Pages.User.CompanyChecks;

public partial class History
{
    private List<CompanyCheckHistoryItem> _items = new();
    private bool _isLoading = true;

    [Inject]
    private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var accessResult = await SubscriptionAccessHelper.EvaluateAccessAsync(_authenticationService, SubscriptionPlanService);

        if (!accessResult.HasUser)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            _isLoading = false;
            return;
        }

        if (!accessResult.HasActiveSubscription)
        {
            _snackbarService.Add(Loc["SubscriptionRequiredMessage"], Severity.Warning);
            _navManager.NavigateTo(NavigationConstants.DashboardUrl, true);
            _isLoading = false;
            return;
        }

        var items = await _companyCheckManager.GetCompletedChecksAsync(accessResult.UserId!.Value);
        _items = items.ToList();
        _isLoading = false;
    }

    private string FormatPaidAt(DateTime? paidAt)
    {
        return paidAt?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "-";
    }

    private async Task ShowDetailsAsync(CompanyCheckHistoryItem item)
    {
        var parameters = new DialogParameters<Dialogs.CompanyCheckDetailDialog>
        {
            { nameof(Dialogs.CompanyCheckDetailDialog.Item), item }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true
        };

        await _dialogService.ShowAsync<Dialogs.CompanyCheckDetailDialog>(Loc["DetailsDialogTitle"], parameters, options);
    }
}
