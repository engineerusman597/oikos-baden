using Oikos.Web.Components.Pages.Admin.User.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;
using MudBlazor;
using static Oikos.Web.Components.Shared.Pages.PagePagination;
using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.Role.Models;
using Oikos.Application.Services.Partner;
using Microsoft.AspNetCore.Components;
using Oikos.Common.Resources;
using Oikos.Common.Constants;

namespace Oikos.Web.Components.Pages.Admin.User;

public partial class ClientUsers
{
    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;
    [Inject] private IPartnerService PartnerService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;


    private List<UserDto> _users = new();
    private SearchObject _searchObject = new();
    private bool _isLoading = true;
    private bool _filtersOpen;
    private List<RoleDto> _roleList = new();
    private List<PartnerListItem> _partnerList = new();
    private int? _adminRoleId;
    private bool _canChangeRole;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
        if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("sys", out var sysValue))
        {
            _canChangeRole = sysValue == "1";
        }

        // Load roles to find Admin role ID
        var rolesResult = await RoleManagementService.GetRolesAsync(new RoleSearchCriteria { PageSize = 1000 });
        _roleList = rolesResult.Items;
        var adminRole = _roleList.FirstOrDefault(r => r.Name == RoleNames.Admin.ToRoleName());
        if (adminRole != null)
        {
            _adminRoleId = adminRole.Id;
        }

        var partners = await PartnerService.GetPartnersAsync();
        _partnerList = partners.Select(p => new PartnerListItem { Id = p.Id, Name = p.Name }).OrderBy(p => p.Name).ToList();

        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        _isLoading = true;
        StateHasChanged();

        var criteria = new UserSearchCriteria
        {
            SearchText = _searchObject.SearchText,
            SearchRealName = _searchObject.SearchRealName,
            ExcludeRoleId = _adminRoleId, // Exclude Admin role
            PartnerId = _searchObject.PartnerId,
            HasActiveSubscription = _searchObject.HasActiveSubscription,
            Page = _searchObject.Page,
            PageSize = _searchObject.Size
        };

        var result = await UserManagementService.GetUsersAsync(criteria);
        _users = result.Items;
        _searchObject.Total = result.TotalCount;

        _isLoading = false;
        StateHasChanged();
    }

    private void ToggleFilters()
    {
        _filtersOpen = !_filtersOpen;
    }

    private async Task ApplyFiltersAsync()
    {
        _searchObject.Page = 1;
        await LoadUsersAsync();
    }

    private async Task PageChangedClick(int page)
    {
        _searchObject.Page = page;
        await LoadUsersAsync();
    }

    private async Task AddUserClick()
    {
        var parameters = new DialogParameters { };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge };
        var dialog = await DialogService.ShowAsync<CreateUserDialog>(Loc["UserPage_CreateNewTitle"], parameters, options);

        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadUsersAsync();
        }
    }

    private async Task DeleteUserClick(int userId)
    {
        await DialogService.ShowDeleteDialog(Loc["UserPage_DeleteTitle"], null,
        async (e) =>
        {
            var (success, errorMessage) = await UserManagementService.DeleteUserAsync(userId);
            
            if (!success)
            {
                var errorKey = errorMessage switch
                {
                    "UserNotFound" => "UserPage_UserNotFound",
                    "HasSubscriptions" => "UserPage_DeleteReasonSubscriptions",
                    "HasInvoices" => "UserPage_DeleteReasonInvoices",
                    "HasInvoiceHistory" => "UserPage_DeleteReasonInvoiceHistory",
                    "HasCompanyChecks" => "UserPage_DeleteReasonCompanyChecks",
                    _ => "UserPage_DeleteError"
                };
                
                SnackbarService.Add(Loc[errorKey], Severity.Error);
                return;
            }

            SnackbarService.Add(Loc["UserPage_DeleteSuccess"], Severity.Success);
            await LoadUsersAsync();
        });
    }

    private async Task EditUserClick(int userId)
    {
        var parameters = new DialogParameters
        {
            {"UserId", userId }
        };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge };
        var dialog = await DialogService.ShowAsync<UpdateUserDialog>(Loc["UserPage_EditTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadUsersAsync();
        }
    }

    private async Task ChangePasswordClick(int userId)
    {
        var parameters = new DialogParameters
        {
            {"UserId", userId }
        };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge };
        var dialog = await DialogService.ShowAsync<ChangePasswordDialog>(Loc["UserPage_ModifyPasswordTitle"], parameters, options);
        await dialog.Result;
    }

    private async Task ChangeUserActive(int userId, bool isEnabled)
    {
        var success = await UserManagementService.ChangeUserStatusAsync(userId, isEnabled);
        if (success)
        {
            SnackbarService.Add(Loc["UserPage_StatusChangedMessage"], Severity.Success);
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.IsEnabled = isEnabled;
            }
        }
    }

    private void SearchReset()
    {
        _searchObject = new();
        _searchObject.Page = 1;
        _ = LoadUsersAsync();
    }

    private record SearchObject : PaginationModel
    {
        public string? SearchText { get; set; }
        public string? SearchRole { get; set; }
        public string? SearchRealName { get; set; }
        public bool? HasActiveSubscription { get; set; }
        public int? PartnerId { get; set; }
    }

    private class PartnerListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
