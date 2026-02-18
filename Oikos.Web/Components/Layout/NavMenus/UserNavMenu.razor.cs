using Microsoft.AspNetCore.Components;
using Oikos.Application.Services.Authentication;
using Oikos.Application.Services.User;
using Oikos.Web.Components.Layout.States;

namespace Oikos.Web.Components.Layout.NavMenus;

public partial class UserNavMenu : IDisposable
{
    [Inject] protected ILayoutState LayoutState { get; set; } = null!;
    [Inject] protected IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] protected IUserManagementService UserManagementService { get; set; } = null!;
    

    protected string? _customerNumber;
    protected string? _avatar;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        LayoutState.NavIsOpenEvent += () => StateHasChanged();

        var userId = await AuthenticationService.GetUserIdAsync();
        var currentUser = await UserManagementService.GetUserAsync(userId);
        if (currentUser != null)
        {

            _avatar = currentUser.Avatar;
            _customerNumber = currentUser.CustomerNumber;
        }
    }
    
    public void Dispose()
    {
         LayoutState.NavIsOpenEvent -= () => StateHasChanged();
    }
}
