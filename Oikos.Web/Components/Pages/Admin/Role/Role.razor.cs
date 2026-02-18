using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.Role.Models;
using Oikos.Web.Components.Pages.Admin.Role.Dialogs;
using static Oikos.Web.Components.Shared.Pages.PagePagination;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.Role;

public partial class Role
{
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;


    private List<RoleDto> _roles = new();
    private RoleSearchCriteria _searchCriteria = new() { Page = 1, PageSize = 10 };
    private PaginationModel _paginationInfo = new() { Page = 1, Size = 10 };
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        _isLoading = true;
        StateHasChanged();

        // Sync search criteria
        _searchCriteria.Page = _paginationInfo.Page;
        _searchCriteria.PageSize = _paginationInfo.Size;

        var result = await RoleManagementService.GetRolesAsync(_searchCriteria);
        _roles = result.Items;
        
        // Sync pagination info
        _paginationInfo.Total = result.TotalCount;

        // Calculate numbers for display
        for (int i = 0; i < _roles.Count; i++)
        {
            _roles[i].Number = (_searchCriteria.Page - 1) * _searchCriteria.PageSize + i + 1;
        }

        _isLoading = false;
        StateHasChanged();
    }

    private async Task ChangeRoleActive(int roleId, bool isEnabled)
    {
        var success = await RoleManagementService.ChangeRoleStatusAsync(roleId, isEnabled);
        if (success)
        {
            SnackbarService.Add(Loc["RolePage_StatusChangedMessage"], Severity.Success);
            var role = _roles.FirstOrDefault(u => u.Id == roleId);
            if (role != null)
            {
                role.IsEnabled = isEnabled;
            }
        }
        else
        {
            SnackbarService.Add(Loc["RolePage_StatusChangeFailed"], Severity.Error);
            // Revert toggle if failed, or reload
            await LoadRolesAsync();
        }
    }

    private async Task AddRoleClick()
    {
        var parameters = new DialogParameters { };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge };
        var dialog = await DialogService.ShowAsync<CreateRoleDialog>(Loc["RolePage_CreateNewTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadRolesAsync();
        }
    }

    private async Task DeleteRoleClick(int roleId)
    {
         
        var parameters = new DialogParameters
        {
            { "ContentText", Loc["RolePage_DeleteConfirm"].Value },
            { "ButtonText", Loc["Delete"] },
            { "Color", Color.Error }
        };

        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small };
        var dialog = await DialogService.ShowAsync<Oikos.Web.Components.Shared.Dialogs.CommonDeleteDialog>(Loc["Delete"], parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            var deleteResult = await RoleManagementService.DeleteRoleAsync(roleId);
            if (deleteResult.Success)
            {
                SnackbarService.Add(Loc["UserPage_DeleteSuccess"], Severity.Success);
                await LoadRolesAsync();
            }
            else
            {
                SnackbarService.Add(deleteResult.ErrorMessage ?? Loc["RolePage_DeleteFailed"], Severity.Error);
            }
        }
    }

    private async Task EditRoleClick(int roleId)
    {
        var parameters = new DialogParameters
        {
            {"RoleId", roleId }
        };
        var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge };
        var dialog = await DialogService.ShowAsync<UpdateRoleDialog>(Loc["RolePage_EditTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled)
        {
           await LoadRolesAsync();
        }
    }

    private async Task PageChangedClick(int page)
    {
        // The PagePagination component updates _paginationInfo.Page
        // We just need to reload.
        // But wait, PagePagination uses EventCallback<int>. 
        // If I look at PagePagination.razor: PageInfo.Page = 1; PageChangedClick.InvokeAsync(PageInfo.Page);
        // It invokes with new page.
        
        _paginationInfo.Page = page;
        await LoadRolesAsync();
    }
}
