using Oikos.Application.Services.Registration;
using Oikos.Application.Services.Registration.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using Oikos.Common.Resources;

using Oikos.Application.Services.Authentication;
using Oikos.Application.Services.Authentication.Models;
using Microsoft.JSInterop;
using Oikos.Domain.Constants;
using Oikos.Web.Auth;

namespace Oikos.Web.Components.Dialogs;

public partial class BonixRegisterDialog
{
    private bool _isLoading = false;
    private RegisterModel _model = new();
    private string? _partnerCode;

    [Inject] private IRegistrationService RegistrationService { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ExternalAuthService ExternalAuthService { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;


    [CascadingParameter] 
    IMudDialogInstance? MudDialog { get; set; }



    protected override void OnInitialized()
    {
        base.OnInitialized();

        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        
        if (query.TryGetValue("partner", out var partnerValues) || query.TryGetValue("partnerCode", out partnerValues))
        {
            _partnerCode = partnerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(_partnerCode))
            {
                _model.PartnerCode = _partnerCode;
            }
        }

        if (query.TryGetValue("email", out var emailValues))
        {
            var email = emailValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(email))
            {
                _model.Email = email;
            }
        }
    }

    public class BonixRegisterResult
    {
        public bool Success { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool LoginRequested { get; set; }
        public bool IsAutoLoggedIn { get; set; }
    }

    private void SwitchToLogin()
    {
        MudDialog?.Close(DialogResult.Ok(new BonixRegisterResult { LoginRequested = true }));
    }

    private void Cancel()
    {
        MudDialog?.Cancel();
    }

    private async Task Register()
    {
        _isLoading = true;
        StateHasChanged();
        await Task.Yield();

        try
        {
            var request = new RegisterUserRequest
            {
                Email = _model.Email ?? string.Empty,
                Password = _model.Password ?? string.Empty,
                Company = _model.Company,
                Gender = _model.Gender,
                Title = _model.Title,
                FirstName = _model.FirstName,
                LastName = _model.LastName,
                PartnerCode = _model.PartnerCode,
                AcceptedPrivacy = _model.AcceptedPrivacy,
                IsBonixUser = true
            };

            var result = await RegistrationService.RegisterUserAsync(request);

            if (result.Success)
            {
                // Auto Login
                try
                {
                    var loginRequest = new LoginRequest
                    {
                        Identifier = result.UserName ?? _model.Email!,
                        Password = result.Password ?? _model.Password!,
                        IpAddress = HttpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                        UserAgent = HttpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
                    };

                    var loginResult = await AuthenticationService.LoginAsync(loginRequest);
                    if (loginResult.Success)
                    {
                         // Set JWT token in cookie
                        try
                        {
                            var cookieUtil = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cookieUtil.js");
                            await cookieUtil.InvokeVoidAsync("setCookie", CommonConstant.UserToken, loginResult.Token);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cookie setting failed: {ex.Message}");
                        }

                        // Set current user state
                        ExternalAuthService.SetCurrentUser(loginResult.Token!);
                        
                        _snackbarService.Add(Loc["Register_Success"], Severity.Success);
                        MudDialog?.Close(DialogResult.Ok(new BonixRegisterResult
                        {
                            Success = true,
                            UserName = result.UserName ?? string.Empty,
                            Password = result.Password ?? string.Empty,
                            IsAutoLoggedIn = true
                        }));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Auto login failed: {ex.Message}");
                    // Fallback to manual login if auto login fails
                }

                _snackbarService.Add(Loc["Register_Success"], Severity.Success);
                MudDialog?.Close(DialogResult.Ok(new BonixRegisterResult
                {
                    Success = true,
                    UserName = result.UserName ?? string.Empty,
                    Password = result.Password ?? string.Empty
                }));
            }
            else
            {
                _snackbarService.Add(result.ErrorMessage ?? Loc["Register_Error"], Severity.Error);
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private class RegisterModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_CompanyRequired")]
        public string? Company { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_GenderRequired")]
        public string? Gender { get; set; }

        public string? Title { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_FirstNameRequired")]
        public string? FirstName { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_LastNameRequired")]
        public string? LastName { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_EmailRequired")]
        [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_EmailInvalid")]
        public string? Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordRequired")]
        [MinLength(6, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordMinLength")]
        public string? Password { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_ConfirmPasswordRequired")]
        [Compare(nameof(Password), ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordMismatch")]
        public string? ConfirmPassword { get; set; }

        public bool AcceptedPrivacy { get; set; }

        [MaxLength(50)]
        public string? PartnerCode { get; set; }
    }
}
