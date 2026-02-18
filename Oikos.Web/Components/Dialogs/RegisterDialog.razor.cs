using Oikos.Application.Services.Registration;
using Oikos.Application.Services.Registration.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using Oikos.Common.Resources;


namespace Oikos.Web.Components.Dialogs;

public partial class RegisterDialog
{
    private bool _isLoading = false;
    private RegisterModel _model = new();
    private string? _partnerCode;

    [Inject]
    private IRegistrationService RegistrationService { get; set; } = null!;

    [CascadingParameter] 
    IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public string? PartnerCode { get; set; }


    protected override void OnInitialized()
    {
        base.OnInitialized();

        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        // First, check if partner code was passed as a parameter
        if (!string.IsNullOrWhiteSpace(PartnerCode))
        {
            _partnerCode = PartnerCode;
            _model.PartnerCode = _partnerCode;
        }
        // Otherwise, try to read from URL query parameters
        else if (query.TryGetValue("partner", out var partnerValues) || query.TryGetValue("partnerCode", out partnerValues))
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

    public class RegisterResult
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
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
                AcceptedPrivacy = _model.AcceptedPrivacy
            };

            var result = await RegistrationService.RegisterUserAsync(request);

            if (result.Success)
            {
                _snackbarService.Add(Loc["Register_Success"], Severity.Success);
                MudDialog?.Close(DialogResult.Ok(new RegisterResult
                {
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

    private void Cancel()
    {
        MudDialog?.Cancel();
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
