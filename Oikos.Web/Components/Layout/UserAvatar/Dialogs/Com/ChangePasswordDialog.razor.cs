using Oikos.Application.Services.User;
using Oikos.Application.Services.User.Models;
using Oikos.Common.Resources;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace Oikos.Web.Components.Layout.UserAvatar.Dialogs.Com;

public partial class ChangePasswordDialog
{
    [Inject] private IUserProfileService _userProfileService { get; set; } = null!;

    [CascadingParameter] IMudDialogInstance? MudDialog { get; set; }


    private PasswordChangeModel PasswordModel = new();

    private async Task Submit()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        var result = await _userProfileService.ChangePasswordAsync(
            userId,
            PasswordModel.CurrentPassword ?? string.Empty,
            PasswordModel.Password ?? string.Empty);

        if (result.Success)
        {
            _snackbarService.Add(Loc["ChangePassword_Success"], Severity.Success);
            MudDialog?.Close(DialogResult.Ok(true));
            return;
        }

        if (result.Error == ChangePasswordError.InvalidCurrentPassword)
        {
            _snackbarService.Add(Loc["ChangePassword_CurrentPasswordInvalid"], Severity.Error);
            return;
        }

        _snackbarService.Add(Loc["ChangePassword_UserMissing"], Severity.Error);
    }

    private record PasswordChangeModel
    {
        [Required(ErrorMessageResourceName = "ChangePassword_CurrentPasswordRequired",
            ErrorMessageResourceType = typeof(SharedResource))]
        [MinLength(4, ErrorMessageResourceName = "ChangePassword_PasswordTooShort",
            ErrorMessageResourceType = typeof(SharedResource))]
        [MaxLength(100, ErrorMessageResourceName = "ChangePassword_PasswordTooLong",
            ErrorMessageResourceType = typeof(SharedResource))]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessageResourceName = "ChangePassword_NewPasswordRequired",
            ErrorMessageResourceType = typeof(SharedResource))]
        [MinLength(4, ErrorMessageResourceName = "ChangePassword_PasswordTooShort",
            ErrorMessageResourceType = typeof(SharedResource))]
        [MaxLength(100, ErrorMessageResourceName = "ChangePassword_PasswordTooLong",
            ErrorMessageResourceType = typeof(SharedResource))]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$",
            ErrorMessageResourceName = "ChangePassword_PasswordComplexity",
            ErrorMessageResourceType = typeof(SharedResource))]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessageResourceName = "ChangePassword_PasswordsDoNotMatch",
            ErrorMessageResourceType = typeof(SharedResource))]
        public string? ConfirmPassword { get; set; }
    }
}
