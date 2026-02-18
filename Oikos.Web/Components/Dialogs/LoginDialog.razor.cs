using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.Authentication.Models;
using Oikos.Common.Resources;
using Oikos.Domain.Constants;
using Oikos.Common.Constants;


namespace Oikos.Web.Components.Dialogs;

public partial class LoginDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

    private LoginDialogModel _loginModel = new();
    private bool _isProcessing;
    private InputType _passwordInputType = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    private void TogglePasswordVisibility()
    {
        if (_passwordInputType == InputType.Password)
        {
            _passwordInputType = InputType.Text;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
        }
        else
        {
            _passwordInputType = InputType.Password;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task LoginSubmit()
    {
        _isProcessing = true;
        try
        {
            var request = new LoginRequest
            {
                Identifier = _loginModel.UserName,
                Password = _loginModel.Password,
                IpAddress = _httpAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = _httpAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
            };

            var result = await _authenticationService.LoginAsync(request);
            if (!result.Success)
            {
                if (result.IsLockedOut)
                {
                     _snackbarService.Add(Loc["Login_AccountLocked"], Severity.Error);
                }
                 else
                {
                    _snackbarService.Add(Loc["Login_InvalidCredentials"], Severity.Error);
                }
                return;
            }

            // Set JWT token in cookie
            try
            {
               var cookieUtil = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cookieUtil.js");
               await cookieUtil.InvokeVoidAsync("setCookie", CommonConstant.UserToken, result.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cookie setting failed: {ex.Message}");
            }

            // Set current user state
            _authService.SetCurrentUser(result.Token!);

             _snackbarService.Add(Loc["Login_Success"], Severity.Success);
             MudDialog.Close(DialogResult.Ok(true));

             // Check if user has User_Bonix role
             if (result.Roles != null && result.Roles.Contains(RoleNames.User_Bonix.ToRoleName()))
             {
                 _navManager.NavigateTo("/user-bonix/dashboard", forceLoad: true);
             }
             else
             {
                 _navManager.NavigateTo("/", forceLoad: true);
             }
        }
        catch (Exception ex)
        {
             _snackbarService.Add(Loc["Login_Error"], Severity.Error);
             Console.WriteLine(ex);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private string GetForgotPasswordLink()
    {
        // Check if we're in a Bonix context by checking the current URL
        var currentUri = _navManager.Uri;
        if (currentUri.Contains("/bonix-auskunft", StringComparison.OrdinalIgnoreCase) || 
            currentUri.Contains("/user-bonix", StringComparison.OrdinalIgnoreCase))
        {
            return "/forgot-password?source=bonix";
        }
        return "/forgot-password";
    }

    public class LoginDialogModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Validation_Required")]
        public string Password { get; set; } = string.Empty;
    }
}
