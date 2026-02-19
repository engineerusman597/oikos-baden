using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Web.Components.Layout.UserAvatar.Dialogs;
using Oikos.Domain.Constants;
using Oikos.Web.Constants;
using Oikos.Application.Services.Authentication;
using Oikos.Web.Auth;
using Oikos.Common.Constants;

namespace Oikos.Web.Components.Layout.UserAvatar;

public partial class UserMenuAvatar
{
    [Inject] protected IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] protected NavigationManager NavManager { get; set; } = null!;
    [Inject] protected IAuthenticationService AuthenticationService { get; set; } = null!;

    [Inject] protected ExternalAuthService ExternalAuthService { get; set; } = null!;

    private string _userName = string.Empty;
    private string? _customerNumber;
    private string? _avatar = string.Empty;
    private bool _isBonixUser = false;
    private bool _isAdmin = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        var userId = await AuthenticationService.GetUserIdAsync();
        var currentUser = await UserManagementService.GetUserAsync(userId);
        if (currentUser != null)
        {
            _userName = currentUser.DisplayName;
            _avatar = currentUser.Avatar;
            _customerNumber = currentUser.CustomerNumber;
        }

        // Check if user is a Bonix user
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _isBonixUser = user.IsInRole(RoleNames.User_Bonix.ToRoleName());
        _isAdmin = user.IsInRole(RoleNames.Admin.ToRoleName());
    }

    private async Task ShowUserSettings()
    {
        var parameters = new DialogParameters { };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, NoHeader = true };
        var dialog = await DialogService.ShowAsync<ProfileSetting>(string.Empty, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadUserDataAsync();
            StateHasChanged();
        }
    }

    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private async Task LogoutClick()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.IsInRole(RoleNames.User_Bonix.ToRoleName()))
        {
            NavManager.NavigateTo("/api/auth/logout?returnUrl=/bonix-auskunft", forceLoad: true);
        }
        else
        {
            NavManager.NavigateTo("/api/auth/logout", forceLoad: true);
        }
    }

    private async Task NavigateToUpgrade()
    {
        await JsRuntime.InvokeVoidAsync("open", ExternalUrlConstants.PricingUrl, "_blank", "noopener,noreferrer");
    }

    private void NavigateToBackToBonix()
    {
        NavManager.NavigateTo("https://www.bonix-auskunft.de/", forceLoad: true);
    }
}
