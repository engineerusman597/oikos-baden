using System.ComponentModel.DataAnnotations;
using Oikos.Common.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Oikos.Web.Components.Pages;

public partial class ResetPassword
{
    [Parameter]
    public string Token { get; set; } = string.Empty;

    private EditContext _editContext = default!;
    private ResetPasswordModel _model = new();
    private bool _isProcessing = true;
    private bool _tokenInvalid;
    private bool _resetCompleted;
    private bool _isSubmitting;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(_model);
    }

    protected override async Task OnParametersSetAsync()
    {
        _isProcessing = true;
        Token = Token?.Trim() ?? string.Empty;
        _tokenInvalid = !await _passwordResetService.IsResetTokenValidAsync(Token);
        _isProcessing = false;
    }

    private async Task SubmitAsync(EditContext editContext)
    {
        var isValid = editContext.Validate();

        if (!isValid)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            var result = await _passwordResetService.ResetPasswordAsync(Token, _model.Password!);
            if (!result)
            {
                _tokenInvalid = true;
                _snackbarService.Add(Loc["ResetPassword_InvalidToken"], Severity.Error);
                return;
            }

            _resetCompleted = true;
        }
        catch (Exception)
        {
            _snackbarService.Add(Loc["ResetPassword_UpdateFailed"], Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private class ResetPasswordModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "ResetPassword_PasswordRequired")]
        public string? Password { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "ResetPassword_ConfirmPasswordRequired")]
        [Compare("Password", ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "ResetPassword_PasswordMismatch")]
        public string? ConfirmPassword { get; set; }
    }
}
