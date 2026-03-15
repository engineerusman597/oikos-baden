using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Oikos.Application.Services.Authentication;
using Oikos.Application.Services.Partner;
using Oikos.Web.Components.Layout.States;

namespace Oikos.Web.Components.Layout.NavMenus;

public partial class PartnerNavMenu : IDisposable
{
    [Inject] private ILayoutState LayoutState { get; set; } = null!;
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;
    [Inject] private IAuthenticationService AuthService { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    private string? _partnerName;
    private bool _isSubPartner;

    protected override async Task OnInitializedAsync()
    {
        LayoutState.NavIsOpenEvent += StateHasChanged;

        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _partnerName = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                        ?? user.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            var userId = await AuthService.GetUserIdAsync();
            _isSubPartner = await PartnerPortalService.IsSubPartnerAsync(userId);
        }
    }

    public void Dispose()
    {
        LayoutState.NavIsOpenEvent -= StateHasChanged;
    }
}
