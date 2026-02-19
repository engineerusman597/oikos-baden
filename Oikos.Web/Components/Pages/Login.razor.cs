using Oikos.Application.Services.Authentication.Models;
using Oikos.Common.Resources;
using Oikos.Application.Services.Newsletter.Models;
using Microsoft.JSInterop;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Oikos.Common.Constants;

namespace Oikos.Web.Components.Pages;

public partial class Login
{
    private Dictionary<string, object> InputAttributes { get; set; } =
        new Dictionary<string, object>()
            {
               { "autocomplete", "new-password2" },
            };

    private bool ShowContent;

    private bool IsLoading = false;

    private bool ShowNewsletterCard = true;

    private LoginModel _loginModel = new();

    private NewsletterModel _newsletterModel = new();

    [SupplyParameterFromQuery(Name = "register")]
    public string? Register { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        var state = await _stateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity != null && state.User.Identity.IsAuthenticated)
        {
            _navManager.NavigateTo("/");
        }
        else
        {
            ShowContent = true;

            // ?register=1 or ?register=true → redirect to dedicated register page
            if (!string.IsNullOrWhiteSpace(Register) &&
                (string.Equals(Register, "1", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Register, "true", StringComparison.OrdinalIgnoreCase)))
            {
                _navManager.NavigateTo("/register");
            }
        }
    }

    private async Task LoginSubmit()
    {
        IsLoading = true;

        try
        {
            var identifier = _loginModel.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                _snackbarService.Add(Loc["Login_UserNameHelpText"], Severity.Error);
                return;
            }

            var ipAddress = _httpAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = _httpAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

            var loginRequest = new LoginRequest
            {
                Identifier = identifier,
                Password = _loginModel.Password ?? string.Empty,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var result = await _authenticationService.LoginAsync(loginRequest);

            if (!result.Success)
            {
                if (result.IsLockedOut && result.LockoutMinutes.HasValue)
                {
                    _snackbarService.Add(
                        string.Format(Loc["Login_TooManyAttempts"].Value, result.LockoutMinutes.Value),
                        Severity.Error);
                }
                else
                {
                    _snackbarService.Add(Loc["Login_InvalidCredentials"], Severity.Error);
                }
                return;
            }

            // Set JWT token in cookie
            var cookieUtil = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cookieUtil.js");
            await cookieUtil.InvokeVoidAsync("setCookie", Oikos.Domain.Constants.CommonConstant.UserToken, result.Token);

            // Set current user and navigate
            _authService.SetCurrentUser(result.Token!);
            
            var state = await _stateProvider.GetAuthenticationStateAsync();
            if (state.User.IsInRole(RoleNames.User_Bonix.ToRoleName()))
            {
                _navManager.NavigateTo("/user-bonix/dashboard");
            }
            else
            {
                _navManager.NavigateTo("/");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CloseNewsletterCard()
    {
        ShowNewsletterCard = false;
        StateHasChanged();
    }

    private void ReopenNewsletterCard()
    {
        ShowNewsletterCard = true;
    }

    private async Task SubscribeToNewsletter()
    {
        if (_newsletterModel.Email is null)
        {
            return;
        }

        try
        {
            var subscriptionRequest = new NewsletterSubscriptionRequest
            {
                Email = _newsletterModel.Email,
                Language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            };

            await _newsletterService.SubscribeAsync(subscriptionRequest);
            _snackbarService.Add(Loc["Login_NewsletterSuccess"], Severity.Success);
            _newsletterModel.Email = string.Empty;
        }
        catch (InvalidOperationException)
        {
            _snackbarService.Add(Loc["ForgotPassword_EmailSettingsMissing"], Severity.Error);
        }
        catch (Exception)
        {
            _snackbarService.Add(Loc["ForgotPassword_EmailSendFailed"], Severity.Error);
        }
    }

    private record LoginModel
    {
        [Required(ErrorMessageResourceName = "Login_UserNameHelpText",
            ErrorMessageResourceType = typeof(SharedResource))]
        public string? UserName { get; set; }

        [Required(ErrorMessageResourceName = "Login_PasswordHelpText",
            ErrorMessageResourceType = typeof(SharedResource))]
        public string? Password { get; set; }
    }

    private class NewsletterModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Login_NewsletterEmailRequired")]
        [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Login_NewsletterEmailInvalid")]
        public string? Email { get; set; }
    }

}
