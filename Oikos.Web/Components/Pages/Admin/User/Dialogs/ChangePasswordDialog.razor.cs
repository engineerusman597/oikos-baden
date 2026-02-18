using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.User;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.User.Dialogs;

public partial class ChangePasswordDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public int UserId { get; set; }
    
    [Inject] private IUserManagementService UserManagementService { get; set; } = null!;
    [Inject] private ISnackbar SnackbarService { get; set; } = null!;


    private PasswordModel _model = new();
    private bool _processing;

    private async Task Submit()
    {
        if (_processing) return;

        _processing = true;

        var success = await UserManagementService.ChangePasswordAsync(UserId, _model.NewPassword!);

        _processing = false;

        if (success)
        {
            SnackbarService.Add(Loc["ChangePasswordDialog_Success"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            SnackbarService.Add(Loc["ChangePasswordDialog_UserNotFound"], Severity.Error);
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private class PasswordModel
    {
        public string? NewPassword { get; set; }
    }
}
