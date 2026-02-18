using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.Role.Models;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.Role.Dialogs;

public partial class UpdateRoleDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public int RoleId { get; set; }
    
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;

    private UpdateRoleModel _model = new();
    private bool _processing;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var role = await RoleManagementService.GetRoleDetailAsync(RoleId);
        
        if (role != null)
        {
            _model = new UpdateRoleModel
            {
                Id = role.Id,
                RoleName = role.Name
            };
        }
        else
        {
            SnackbarService.Add(Loc["UserPage_UserNotFound"], Severity.Error);
            MudDialog.Cancel();
        }
        _loading = false;
    }

    private async Task Submit()
    {
        if (_processing) return;
        _processing = true;

        var request = new UpdateRoleRequest
        {
            Name = _model.RoleName!
        };

        var success = await RoleManagementService.UpdateRoleAsync(RoleId, request);

        _processing = false;

        if (success)
        {
            SnackbarService.Add(Loc["UserPage_UpdateSuccess"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            SnackbarService.Add(Loc["RoleNameInUse"], Severity.Error);
        }
    }
    
    private void Cancel()
    {
         MudDialog.Cancel();
    }

    private class UpdateRoleModel
    {
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        [MaxLength(200, ErrorMessageResourceName = "Validation_MaxLength", ErrorMessageResourceType = typeof(SharedResource))]
        public string? RoleName { get; set; }
    }
}
