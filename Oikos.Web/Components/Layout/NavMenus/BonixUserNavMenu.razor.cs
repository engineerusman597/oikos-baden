using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Oikos.Web.Components.Layout.States;

namespace Oikos.Web.Components.Layout.NavMenus;

public partial class BonixUserNavMenu : IDisposable
{
    [Inject] private ILayoutState LayoutState { get; set; } = null!;
    
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = null!;

    private string? _customerNumber;

    protected override async Task OnInitializedAsync()
    {
        LayoutState.NavIsOpenEvent += StateHasChanged;

        var authState = await AuthenticationStateTask;
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
             _customerNumber = user.Claims.FirstOrDefault(c => c.Type == "CustomerNumber")?.Value;
        }
    }

    public void Dispose()
    {
        LayoutState.NavIsOpenEvent -= StateHasChanged;
    }
}
