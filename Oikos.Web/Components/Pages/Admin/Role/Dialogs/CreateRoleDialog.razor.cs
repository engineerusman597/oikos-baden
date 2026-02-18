using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Role;
using Oikos.Application.Services.Role.Models;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.Role.Dialogs;

public partial class CreateRoleDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IRoleManagementService RoleManagementService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;

    private CreateRoleModel _model = new();
    private bool _processing;

    private async Task Submit()
    {
        if (_processing) return;
        _processing = true;

        var request = new CreateRoleRequest
        {
            Name = _model.RoleName!
        };

        var success = await RoleManagementService.CreateRoleAsync(request);

        _processing = false;

        if (success)
        {
            SnackbarService.Add(Loc["UserPage_CreateSuccess"], Severity.Success);
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

    private class CreateRoleModel
    {
        [Required(ErrorMessageResourceName = "Validation_Required", ErrorMessageResourceType = typeof(SharedResource))]
        [MaxLength(200, ErrorMessageResourceName = "Validation_MaxLength", ErrorMessageResourceType = typeof(SharedResource))]
        public string? RoleName { get; set; }
    }
}
