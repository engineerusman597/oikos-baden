using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Domain.Entities.CompanyCheck;
using Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;
using Oikos.Application.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Oikos.Application.Services.Authentication;

namespace Oikos.Web.Components.Pages.User_Bonix.CompanyChecks;

public partial class Index
{
    [Inject] private ILogger<Index> Logger { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] private IAuthenticationService AuthenticationService { get; set; } = null!;

    private readonly WizardStep[] _steps =
    {
        new("WizardStepSearch", Icons.Material.Filled.Search),
        new("WizardStepReview", Icons.Material.Filled.Domain),
        new("WizardStepPayment", Icons.Material.Filled.Payment), // Changed from Confirmation
        new("WizardStepDetails", Icons.Material.Filled.Assignment)
    };

    private readonly CompanySearchCriteria _searchModel = new();
    private List<CreditSafeCompanySummary> _searchResults = new();
    private CreditSafeCompanySummary? _selectedCompany;
    private CompanyCheckRequest? _currentRequest;
    private CompanyCheckReport? _report;

    private bool _isSearching;
    private bool _searchExecuted;
    private bool _requestCreated;
    private bool _reportReady;
    private bool _isGeneratingPdf;
    private bool _creditsafeConfigured = true;
    private bool _showAdvancedFilters;
    private bool _isProcessingPayment;

    private string? _errorMessage;
    private string? _searchReason;

    private int _currentStep;
    private bool _shouldTriggerAutomaticDownload;
    private bool _automaticDownloadTriggered;
    private readonly string[] _companyStatuses = { "Active", "NonActive", "Pending", "Other" };
    private readonly string[] _companyTypes = { "Ltd", "NonLtd" };

    private int? _userId;
    private decimal? _price;
    private string? _currency;

    protected override async Task OnInitializedAsync()
    {
        _userId = await GetCurrentUserIdAsync();
        var initResult = await _wizardService.InitializeAsync(_userId);
        _creditsafeConfigured = initResult.IsConfigured;
        _searchModel.Country ??= initResult.DefaultCountry;
        _price = initResult.Price;
        _currency = initResult.Currency;
    }

    private async Task OnSearchFormSubmittedAsync()
    {
        if (_isSearching || !_creditsafeConfigured)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_searchReason))
        {
            var confirmed = await PromptForSearchPurposeAsync();
            if (!confirmed)
            {
                return;
            }
        }

        await SearchAsync();
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
            var dialogReference = await _dialogService.ShowAsync<CompanySearchPurposeDialog>(
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
        }

        return false;
    }

    private async Task SearchAsync()
    {
        _errorMessage = null;
        _isSearching = true;

        try
        {
            var searchRequest = new CompanySearchRequest(
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

            var response = await _wizardService.SearchCompaniesAsync(searchRequest);
            _creditsafeConfigured = response.IsConfigured;

            if (!_creditsafeConfigured)
            {
                _errorMessage = Loc["CreditSafeMissingConfiguration"];
                return;
            }

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            {
                _errorMessage = response.ErrorMessage;
                return;
            }

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

            _searchExecuted = true;
            _selectedCompany = null;
            _errorMessage = null;
            _currentStep = 1;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isSearching = false;
        }
    }

    private async Task OnSelectCompanyClickedAsync(CreditSafeCompanySummary company)
    {
        if (company is null)
        {
            return;
        }

        var historyItem = new CompanyCheckHistoryItem
        {
            CompanyName = company.Name,
            CountryCode = company.CountryCode,
            RegistrationNumber = company.RegistrationNumber,
            SelectedCompany = company
        };

        var parameters = new DialogParameters<CompanyCheckDetailDialog>
        {
            { nameof(CompanyCheckDetailDialog.Item), historyItem },
            { nameof(CompanyCheckDetailDialog.ShowPurchaseSummary), false },
            { nameof(CompanyCheckDetailDialog.AllowSelection), true },
            { nameof(CompanyCheckDetailDialog.SelectButtonLabel), Loc["SearchResultsSelectButton"].Value }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true
        };

        try
        {
            var dialogReference = await _dialogService.ShowAsync<CompanyCheckDetailDialog>(
                Loc["SearchResultsDetailsHeading"],
                parameters,
                options);

            var result = await dialogReference.Result;

            if (!result.Canceled && result.Data is CompanyCheckHistoryItem selectedItem && selectedItem.SelectedCompany is not null)
            {
                _selectedCompany = selectedItem.SelectedCompany;
                _currentStep = 2; // Move to Payment Summary
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ProceedToPaymentAsync()
    {
        if (_selectedCompany is null)
        {
            return;
        }

        _isProcessingPayment = true;
        try
        {
            var userInfo = await AuthenticationService.GetUserInfoAsync();
            var userEmail = userInfo?.Email ?? string.Empty;

            var orderRequest = new CreateOrderRequest(
                _userId,
                _searchModel.CompanyName ?? _selectedCompany.Name,
                _selectedCompany.CompanyId ?? string.Empty,
                _selectedCompany.Name ?? string.Empty,
                _selectedCompany.CountryCode,
                _selectedCompany.RegistrationNumber,
                _price ?? 20m,
                _currency ?? "EUR",
                userEmail,
                System.Text.Json.JsonSerializer.Serialize(_selectedCompany),
                _searchReason
            );

            var result = await _wizardService.CreatePendingOrderAsync(orderRequest);

            if (!result.Success || result.Request is null)
            {
                _snackbarService.Add(result.ErrorMessage ?? Loc["ErrorTitle"], Severity.Error);
                return;
            }

            _currentRequest = result.Request;
            
            // Redirect to Stripe
            var successUrl = _navManager.ToAbsoluteUri($"/user-bonix/company-checks/success?requestId={result.Request.Id}&session_id={{CHECKOUT_SESSION_ID}}").ToString();
            var cancelUrl = _navManager.ToAbsoluteUri("/user-bonix/company-checks").ToString();

            Logger.LogInformation("Creating Stripe checkout with Success URL: {SuccessUrl}", successUrl);
            Logger.LogInformation("Creating Stripe checkout with Cancel URL: {CancelUrl}", cancelUrl);

            var checkoutUrl = await _wizardService.CreateStripeCheckoutSessionAsync(result.Request.Id, successUrl, cancelUrl);

            if (!string.IsNullOrEmpty(checkoutUrl))
            {
                Logger.LogInformation("Redirecting to Stripe checkout: {CheckoutUrl}", checkoutUrl);
                _navManager.NavigateTo(checkoutUrl);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _isProcessingPayment = false;
        }
    }

    // Success confirmation logic will be handled by the redirect page or logic here if params are present
    // But typical flow is redirect. Here user asked for "similar to CompanyChecks but with payment".
    // I will implementation report loading assuming we landed back or it's just the search part.
    // Actually, I need to handle the success return. I'll probably need a separate Success page or handle OnInitialized with query params.
    // For now, I'll keep the standard methods for report viewing if needed.

    private string? _reportDownloadUrl;

    // ... (Report generation and download logic similar to Admin, reusable if viewing history)
    
    private void NavigateToHistory()
    {
        _navManager.NavigateTo("/user-bonix/company-checks/history");
    }

    private int GetMaxAvailableStep()
    {
        var max = 0;
        if (_searchExecuted) max = 1;
        if (_requestCreated || _currentStep == 2) max = 2; // Payment step
        if (_reportReady) max = 3;
        return max;
    }

    private void NavigateToStep(int step)
    {
        var maxStep = GetMaxAvailableStep();
        if (step < 0 || step > maxStep) return;
        _currentStep = step;
        StateHasChanged();
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
         var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
         return authState.User.GetUserId();
    }

    private sealed record WizardStep(string TitleKey, string Icon);
}
