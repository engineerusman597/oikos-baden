using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.Role.Models;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.User.Dialogs;

public partial class UserRoleDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public int UserId { get; set; }
    
    [Inject] private IUserRoleService UserRoleService { get; set; } = null!;
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;


    private List<RoleDto> _roleList = new();
    private Dictionary<int, bool> _checkedRoles = new();
    private bool _loading = true;
    private bool _processing;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Load all available roles
        var rolesResult = await RoleManagementService.GetRolesAsync(new RoleSearchCriteria { PageSize = 1000 });
        _roleList = rolesResult.Items.Where(r => r.IsEnabled).ToList();

        // Load user's current roles
        var userRoles = (await UserRoleService.GetUserRolesAsync(UserId)).Select(r => r.Id).ToList();

        // Initialize checked state
        foreach (var role in _roleList)
        {
            _checkedRoles[role.Id] = userRoles.Contains(role.Id);
        }

        _loading = false;
    }

    private async Task Submit()
    {
        if (_processing) return;

        _processing = true;

        var selectedRoleIds = _checkedRoles.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        await UserRoleService.UpdateUserRolesAsync(UserId, selectedRoleIds);

        _processing = false;

        SnackbarService.Add(Loc["UserPage_RoleUpdateSuccess"], Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
