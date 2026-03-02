using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Oikos.Web.Components.Layout.States;

namespace Oikos.Web.Components.Layout.NavMenus;

public partial class PartnerNavMenu : IDisposable
{
    [Inject] private ILayoutState LayoutState { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    private string? _partnerName;

    protected override async Task OnInitializedAsync()
    {
        LayoutState.NavIsOpenEvent += StateHasChanged;

        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _partnerName = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                        ?? user.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
        }
    }

    public void Dispose()
    {
        LayoutState.NavIsOpenEvent -= StateHasChanged;
    }
}
