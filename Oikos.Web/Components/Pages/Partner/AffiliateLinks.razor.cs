using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Oikos.Application.Services.Partner;

namespace Oikos.Web.Components.Pages.Partner;

public partial class AffiliateLinks
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;

    private string? _referralCode;
    private string _affiliateUrl = string.Empty;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        var dashboard = await PartnerPortalService.GetDashboardAsync(userId);
        if (dashboard is not null)
        {
            _referralCode = dashboard.ReferralCode;
            _affiliateUrl = $"{_navManager.BaseUri}register?ref={_referralCode}";
        }
        _isLoading = false;
    }

    private async Task CopyLinkAsync()
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _affiliateUrl);
        _snackbarService.Add(Loc["PartnerAffiliate_Copied"], MudBlazor.Severity.Success);
    }
}
