using MudBlazor;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Web.Components.Shared.Dialogs;
using Oikos.Web.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Oikos.Application.Extensions;

namespace Oikos.Web.Components.Pages.User_Bonix;

public partial class QuickCompanyCheck
{
    private int _step = 0;
    private CompanySearchCriteria _searchModel = new();
    private List<CreditSafeCompanySummary> _searchResults = new();
    private CreditSafeCompanySummary? _selectedCompany;
    private string _email = string.Empty;
    private bool _isSearching;
    private bool _isProcessing;
    
    // Injections
    [Inject] IDialogService DialogService { get; set; } = null!;
    [Inject] NavigationManager NavigationManager { get; set; } = null!;
    [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] ISnackbar SnackbarService { get; set; } = null!;
    [Inject] ICompanyCheckWizardService WizardService { get; set; } = null!;

    private decimal? _price;
    private string? _currency;
    private string? _searchReason;
    private readonly string[] _companyStatuses = { "Active", "NonActive", "Pending", "Other" };
    private readonly string[] _companyTypes = { "Ltd", "NonLtd" };

    protected override async Task OnInitializedAsync()
    {
        var result = await WizardService.InitializeAsync(null);
        _price = result.Price;
        _currency = result.Currency;
    }

    private async Task OpenRegisterDialog()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var parameters = new DialogParameters<BonixRegisterDialog>();

        var dialog = await DialogService.ShowAsync<BonixRegisterDialog>(Loc["Login_CreateAccountDialogTitle"], parameters, options);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data is BonixRegisterDialog.BonixRegisterResult rr)
        {
            if (rr.IsAutoLoggedIn)
            {
                // Already logged in, maybe refresh or just close
                NavigationManager.NavigateTo("/user-bonix/dashboard", forceLoad: true);
            }
            else if (rr.LoginRequested || rr.Success)
            {
                 await OpenLoginDialog();
            }
        }
    }

    private async Task OpenLoginDialog()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };
        var dialog = await DialogService.ShowAsync<LoginDialog>(Loc["Login_Title"], options);
        await dialog.Result;
        // No redirection, auth state updates automatically
    }

    private async Task<bool> PromptForSearchPurposeAsync()
    {
        var parameters = new DialogParameters<CompanySearchPurposeDialog>
        {
            { nameof(CompanySearchPurposeDialog.SelectedPurpose), _searchReason }
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        try
        {
            var dialogReference = await DialogService.ShowAsync<CompanySearchPurposeDialog>(
                Loc["SearchPurposeTitle"],
                parameters,
                options);

            var result = await dialogReference.Result;
            if (!result.Canceled && result.Data is string selectedReason && !string.IsNullOrWhiteSpace(selectedReason))
            {
                _searchReason = selectedReason;
                return true;
            }
        }
        catch (TaskCanceledException)
        {
            // Dialog closed before returning a result
        }

        return false;
    }

    private async Task SearchAsync()
    {
        // Only prompt for search purpose on first search
        if (string.IsNullOrWhiteSpace(_searchReason))
        {
            var confirmed = await PromptForSearchPurposeAsync();
            if (!confirmed)
            {
                return;
            }
        }

        _isSearching = true;
        try
        {
            // Create request object as required by ICompanyCheckWizardService
            var request = new CompanySearchRequest(
                _searchModel.CompanyName,
                _searchModel.Country,
                _searchModel.RegistrationNumber,
                _searchModel.VatNumber,
                _searchModel.Street,
                _searchModel.City,
                _searchModel.PostCode,
                _searchModel.PhoneNumber,
                _searchModel.Status,
                _searchModel.CompanyType
            );

            var response = await WizardService.SearchCompaniesAsync(request);
            if (response.IsConfigured && string.IsNullOrEmpty(response.ErrorMessage))
            {
                // Map results to summary
                _searchResults = response.Results.Select(dto => new CreditSafeCompanySummary
                {
                    CompanyId = dto.CompanyId,
                    Name = dto.Name,
                    CountryCode = dto.CountryCode,
                    RegistrationNumber = dto.RegistrationNumber,
                    Address = dto.Address,
                    Status = dto.Status,
                    Type = dto.Type,
                    LatestAccountsDate = dto.LatestAccountsDate
                }).ToList();

                _step = 1;
            }
            else
            {
                SnackbarService.Add(response.ErrorMessage ?? "Search failed", Severity.Error);
            }
        }
        catch (Exception ex)
        {
             SnackbarService.Add("An unexpected error occurred.", Severity.Error);
             Console.WriteLine(ex);
        }
        finally
        {
            _isSearching = false;
        }
    }

    private void SelectCompany(CreditSafeCompanySummary company)
    {
        _selectedCompany = company;
        _step = 2;
    }

    private void BackToSearch()
    {
        _step = 0;
        _searchResults.Clear();
    }
    
    private void BackToResults()
    {
        _step = 1;
        _selectedCompany = null;
    }

    private async Task ProceedToPaymentAsync()
    {
        if (_selectedCompany == null || string.IsNullOrWhiteSpace(_email)) return;
        
        _isProcessing = true;
        try 
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            int? userId = null;
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                 userId = authState.User.GetUserId();
            }

            var request = new CreateOrderRequest(
                UserId: userId,
                RequestedCompanyName: _searchModel.CompanyName ?? string.Empty,
                CompanyId: _selectedCompany.CompanyId,
                CompanyName: _selectedCompany.Name ?? string.Empty,
                CountryCode: _selectedCompany.CountryCode,
                RegistrationNumber: _selectedCompany.RegistrationNumber,
                Price: _price ?? 20m, 
                Currency: _currency ?? "EUR",
                UserEmail: _email,
                SelectedCompanyJson: null,
                SearchReason: _searchReason
            );

            // Create Pending Order first
            var result = await WizardService.CreatePendingOrderAsync(request);
            if (result.Success && result.Request != null)
            {
                var successUrl = NavigationManager.ToAbsoluteUri($"/bonix-auskunft/success?requestId={result.Request.Id}&session_id={{CHECKOUT_SESSION_ID}}").ToString();
                var cancelUrl = NavigationManager.ToAbsoluteUri("/bonix-auskunft").ToString();

                // Create Stripe Session
                var checkoutUrl = await WizardService.CreateStripeCheckoutSessionAsync(result.Request.Id, successUrl, cancelUrl);
                
                if (!string.IsNullOrEmpty(checkoutUrl))
                {
                    NavigationManager.NavigateTo(checkoutUrl);
                }
            }
            else
            {
                 SnackbarService.Add(result.ErrorMessage ?? "Order creation failed", Severity.Error);
            }
        }
        catch (Exception ex)
        {
             SnackbarService.Add(Loc["Error_PaymentInit"], Severity.Error);
             Console.WriteLine(ex);
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
