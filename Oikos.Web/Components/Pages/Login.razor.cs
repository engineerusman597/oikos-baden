using Oikos.Application.Services.Authentication.Models;
using Oikos.Common.Resources;
using Oikos.Application.Services.Newsletter.Models;
using Oikos.Web.Components.Dialogs;
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

    private bool _registerDialogOpened;

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
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!_registerDialogOpened && ShouldOpenRegisterDialog() && ShowContent)
        {
            _registerDialogOpened = true;
            await OpenRegisterDialog();
        }
    }

    private bool ShouldOpenRegisterDialog()
    {
        if (string.IsNullOrWhiteSpace(Register))
        {
            return false;
        }

        return string.Equals(Register, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Register, "true", StringComparison.OrdinalIgnoreCase);
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

    private async Task OpenRegisterDialog()
    {
        // Extract partner code from returnUrl if present
        string? partnerCode = null;
        var uri = new Uri(_navManager.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        
        if (query.TryGetValue("returnUrl", out var returnUrlValues))
        {
            var returnUrl = returnUrlValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                try
                {
                    var returnUri = new Uri(returnUrl, UriKind.RelativeOrAbsolute);
                    if (!returnUri.IsAbsoluteUri)
                    {
                        returnUri = new Uri(_navManager.BaseUri.TrimEnd('/') + returnUrl);
                    }
                    var returnQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(returnUri.Query);
                    if (returnQuery.TryGetValue("partner", out var partnerValues) || 
                        returnQuery.TryGetValue("partnerCode", out partnerValues))
                    {
                        partnerCode = partnerValues.FirstOrDefault();
                    }
                }
                catch
                {
                    // If parsing fails, continue without partner code
                }
            }
        }

        var parameters = new DialogParameters<RegisterDialog>();
        if (!string.IsNullOrWhiteSpace(partnerCode))
        {
            parameters.Add(nameof(RegisterDialog.PartnerCode), partnerCode);
        }

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var dialog = await _dialogService.ShowAsync<RegisterDialog>(Loc["Login_CreateAccountDialogTitle"], parameters, options);
        var result = await dialog.Result;
        if (!result.Canceled && result.Data is RegisterDialog.RegisterResult rr)
        {
            _loginModel.UserName = rr.UserName;
            _loginModel.Password = rr.Password;
            await LoginSubmit();
        }
    }
}
