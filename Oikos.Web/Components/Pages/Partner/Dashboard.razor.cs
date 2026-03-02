using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Partner.Models;

namespace Oikos.Web.Components.Pages.Partner;

public partial class Dashboard
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;

    private PartnerPortalDashboardDto? _dashboard;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        _dashboard = await PartnerPortalService.GetDashboardAsync(userId);
        _isLoading = false;
    }

    private async Task CopyReferralCodeAsync()
    {
        if (_dashboard is null) return;
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _dashboard.ReferralCode);
        _snackbarService.Add(Loc["PartnerDashboard_ReferralCode_Copied"], MudBlazor.Severity.Success);
    }
}
