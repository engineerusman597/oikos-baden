using Microsoft.AspNetCore.Components;
using Oikos.Application.Services.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;

namespace Oikos.Web.Components.Layout.UserAvatar.Dialogs.Com;

public partial class BasicInfoConfig
{
    [Inject] private IUserProfileService _userProfileService { get; set; } = null!;

    private UserProfileDetails _user = new(
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null);
    private string _activePlanName = string.Empty;
    private string _planExpirationText = string.Empty;
    private bool _hasActiveSubscription;

    [Inject]
    private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var userId = await _authenticationService.GetUserIdAsync();
        _user = await _userProfileService.GetProfileDetailsAsync(userId) ?? _user;

        await LoadActiveSubscriptionAsync(userId);
    }

    private async Task ChangePwd()
    {
        var parameters = new DialogParameters { };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, NoHeader = true };
        var dialog = await _dialogService.ShowAsync<ChangePasswordDialog>(string.Empty, parameters, options);
        await dialog.Result;
    }

    private async Task UploadFiles(IBrowserFile file)
    {
        var parameters = new DialogParameters
        {
            {"BrowserFile",file }
        };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, NoHeader = false };
        var dialog = await _dialogService.ShowAsync<AvatarEditDialog>(Loc["AccountSettings_EditAvatarTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            var userId = await _authenticationService.GetUserIdAsync();
            _user = await _userProfileService.GetProfileDetailsAsync(userId) ?? _user;
            StateHasChanged();
        }
    }

    private async Task LoadActiveSubscriptionAsync(int userId)
    {
        var subscription = await SubscriptionPlanService.GetActiveSubscriptionAsync(userId);
        _hasActiveSubscription = subscription != null;
        
        _activePlanName = string.IsNullOrWhiteSpace(subscription?.PlanName)
            ? Loc["AccountSettings_NotAssigned"]
            : subscription.PlanName;
        _planExpirationText = subscription?.ExpirationDate?.ToString("yyyy-MM-dd")
            ?? Loc["AccountSettings_NoExpiration"];
    }
}
