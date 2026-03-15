using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.Partner;
using Oikos.Application.Services.Partner.Models;

namespace Oikos.Web.Components.Pages.Partner;

public partial class SubPartners
{
    [Inject] private IPartnerPortalService PartnerPortalService { get; set; } = null!;

    private List<PartnerSubPartnerDto> _subPartners = new();
    private bool _isLoading = true;

    private bool _showDialog;
    private bool _isSaving;
    private string? _createError;
    private readonly DialogOptions _dialogOptions = new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    private CreateSubPartnerFormModel _form = new();
    private int _currentUserId;
    private bool _isSubPartner;

    protected override async Task OnInitializedAsync()
    {
        _currentUserId = await _authenticationService.GetUserIdAsync();
        _isSubPartner = await PartnerPortalService.IsSubPartnerAsync(_currentUserId);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _isLoading = true;
        _subPartners = await PartnerPortalService.GetSubPartnersAsync(_currentUserId);
        _isLoading = false;
    }

    private void OpenCreateDialog()
    {
        _form = new CreateSubPartnerFormModel();
        _createError = null;
        _showDialog = true;
    }

    private void CloseDialog()
    {
        _showDialog = false;
        _createError = null;
    }

    private async Task CreateSubPartnerAsync()
    {
        if (string.IsNullOrWhiteSpace(_form.Name) || string.IsNullOrWhiteSpace(_form.Email) || string.IsNullOrWhiteSpace(_form.Password))
        {
            _createError = Loc["PartnerSub_ValidationRequired"];
            return;
        }

        _isSaving = true;
        _createError = null;

        var request = new CreateSubPartnerRequest(
            Name: _form.Name,
            Business: _form.Business,
            Email: _form.Email,
            PhoneNumber: _form.PhoneNumber,
            CommissionType: _form.CommissionType,
            CommissionRate: _form.CommissionRate,
            Password: _form.Password);

        var (success, error) = await PartnerPortalService.CreateSubPartnerAsync(_currentUserId, request);

        if (success)
        {
            _snackbarService.Add(Loc["PartnerSub_CreateSuccess"], MudBlazor.Severity.Success);
            _showDialog = false;
            await LoadAsync();
        }
        else
        {
            _createError = error ?? Loc["PartnerSub_CreateError"];
        }

        _isSaving = false;
    }

    private async Task CopyCodeAsync(string code)
    {
        await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", code);
        _snackbarService.Add(Loc["PartnerDashboard_ReferralCode_Copied"], MudBlazor.Severity.Success);
    }

    private sealed class CreateSubPartnerFormModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Business { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string CommissionType { get; set; } = "Monthly";
        public decimal CommissionRate { get; set; } = 10m;
        public string Password { get; set; } = string.Empty;
    }
}
