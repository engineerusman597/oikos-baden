using System.ComponentModel.DataAnnotations;
using Oikos.Common.Resources;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Oikos.Web.Components.Pages;

public partial class ForgotPassword
{
    private EditContext _editContext = default!;
    private ForgotPasswordModel _model = new();
    private bool _isSubmitting;
    private bool _requestCompleted;

    [Parameter]
    [SupplyParameterFromQuery(Name = "source")]
    public string? Source { get; set; }

    protected override void OnInitialized()
    {
        _editContext = new EditContext(_model);
    }

    private async Task SubmitAsync()
    {
        if (_isSubmitting)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            var isBonixSource = string.Equals(Source, "bonix", StringComparison.OrdinalIgnoreCase);
            await _passwordResetService.RequestPasswordResetAsync(_model.Email!, _navManager.BaseUri, isBonixSource);
            _requestCompleted = true;
        }
        catch (InvalidOperationException)
        {
            _snackbarService.Add(Loc["ForgotPassword_EmailSettingsMissing"], Severity.Error);
        }
        catch (Exception)
        {
            _snackbarService.Add(Loc["ForgotPassword_EmailSendFailed"], Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private string GetBackLink()
    {
        return string.Equals(Source, "bonix", StringComparison.OrdinalIgnoreCase) 
            ? "/bonix-auskunft" 
            : "/login";
    }

    private class ForgotPasswordModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "ForgotPassword_EmailRequired")]
        [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "ForgotPassword_EmailInvalid")]
        public string? Email { get; set; }
    }
}
