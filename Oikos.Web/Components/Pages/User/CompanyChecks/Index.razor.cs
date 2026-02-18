using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Domain.Entities.CompanyCheck;
using System.Globalization;
using Oikos.Application.Services.Subscription;
using Oikos.Web.Constants;

namespace Oikos.Web.Components.Pages.User.CompanyChecks;

using Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;

public partial class Index
{
    [Inject] private ILogger<Index> Logger { get; set; } = null!;
    [Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;

    private readonly WizardStep[] _steps =
    {
        new("WizardStepSearch", Icons.Material.Filled.Search),
        new("WizardStepReview", Icons.Material.Filled.Domain),
        new("WizardStepConfirmation", Icons.Material.Filled.TaskAlt),
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
    private AccessState _accessState = AccessState.Loading;
    private bool _accessInitialized;
    private bool _subscriptionBlocked;
    private int? _currentUserId;
    private bool _hasStoredMandate;
    private bool _pendingConfirmationAfterSepa;

    private decimal? _price;
    private string? _currency;
    private string? _errorMessage;
    private string? _searchReason;

    private SepaMandateDetails _sepaMandateModel = new();
    private EditContext? _sepaMandateEditContext;
    private IJSObjectReference? _commonJsModule;

    private int _currentStep;
    private bool _shouldTriggerAutomaticDownload;
    private bool _automaticDownloadTriggered;
    private readonly string[] _companyStatuses = { "Active", "NonActive", "Pending", "Other" };
    private readonly string[] _companyTypes = { "Ltd", "NonLtd" };

    private int? _lastEmailedRequestId;

    private string _priceDisplay => string.Format(
        CultureInfo.CurrentCulture,
        "{0} {1}",
        (_price ?? 0m).ToString("F2", CultureInfo.CurrentCulture),
        (_currency ?? "EUR").ToUpperInvariant());

    protected override async Task OnInitializedAsync()
    {
        if (!await EnsureActiveSubscriptionAsync())
        {
            _subscriptionBlocked = true;
            return;
        }

        var initResult = await _wizardService.InitializeAsync(_currentUserId);
        _creditsafeConfigured = initResult.IsConfigured;
        _searchModel.Country ??= initResult.DefaultCountry;
        _price = initResult.Price;
        _currency = initResult.Currency ?? "EUR";
        
        await InitializeSepaMandateAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_subscriptionBlocked)
        {
            return;
        }

        if (firstRender && !_accessInitialized)
        {
            await InitializeAccessStateAsync();
            _accessInitialized = true;
            StateHasChanged();
        }
    }

    private async Task<bool> EnsureActiveSubscriptionAsync()
    {
        var accessResult = await SubscriptionAccessHelper.EvaluateAccessAsync(
            _authenticationService,
            SubscriptionPlanService,
            Logger);

        if (!accessResult.HasUser)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            _navManager.NavigateTo(NavigationConstants.DashboardUrl);
            return false;
        }

        if (!accessResult.HasActiveSubscription)
        {
            _snackbarService.Add(Loc["SubscriptionRequiredMessage"], Severity.Warning);
            _navManager.NavigateTo(NavigationConstants.DashboardUrl, true);
            return false;
        }

        _currentUserId = accessResult.UserId;
        return true;
    }

    private async Task InitializeAccessStateAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        _accessState = AccessState.Ready;
        _hasStoredMandate = false;

        if (!userId.HasValue)
        {
            return;
        }

        try
        {
            _hasStoredMandate = await _wizardService.HasStoredSepaMandateAsync(userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to resolve stored SEPA mandate for user {UserId}", userId);
        }
    }

    private async Task OnSearchFormSubmittedAsync()
    {
        if (_isSearching || !_creditsafeConfigured)
        {
            return;
        }

        // Prompt for search purpose only if not already selected
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
            // Dialog closed before returning a result
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

            // Map DTOs back to service models for UI compatibility
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
                await ProceedToConfirmationAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // dialog was disposed before returning a result, nothing to do
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ProceedToConfirmationAsync()
    {
        if (_selectedCompany is null)
        {
            return;
        }

        if (_price is null)
        {
            _snackbarService.Add(Loc["MissingPriceError"], Severity.Error);
            return;
        }

        if (!_hasStoredMandate)
        {
            _pendingConfirmationAfterSepa = true;
            _accessState = AccessState.Introduction;
            await InvokeAsync(StateHasChanged);
            return;
        }

        var userId = await GetCurrentUserIdAsync();
        if (userId is null)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            return;
        }

        var email = await GetCurrentUserEmailAsync();
        try
        {
            var requestedName = string.IsNullOrWhiteSpace(_searchModel.CompanyName)
                ? _selectedCompany.Name
                : _searchModel.CompanyName;

            var orderRequest = new Oikos.Application.Services.CompanyCheck.Models.CreateOrderRequest(
                userId.Value,
                requestedName,
                _selectedCompany.CompanyId ?? string.Empty,
                _selectedCompany.Name ?? string.Empty,
                _selectedCompany.CountryCode,
                _selectedCompany.RegistrationNumber,
                _price.Value,
                _currency ?? "EUR",
                email,
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
            _requestCreated = true;
            _currentStep = 2;
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
    }

    private async Task ProceedToSepaConsentAsync()
    {
        await InitializeSepaMandateAsync();
        _accessState = AccessState.SepaConsent;
        await InvokeAsync(StateHasChanged);
    }

    private async Task InitializeSepaMandateAsync()
    {
        var userName = await GetCurrentUserNameAsync();
        var email = await GetCurrentUserEmailAsync();

        _sepaMandateModel = new SepaMandateDetails
        {
            CompanyName = _selectedCompany?.Name ?? _searchModel.CompanyName ?? string.Empty,
            LegalForm = _selectedCompany?.Type ?? string.Empty,
            AuthorizedRepresentative = userName ?? string.Empty,
            AccountHolderName = userName ?? string.Empty,
            Email = email ?? string.Empty,
            Country = CultureInfo.CurrentCulture?.TwoLetterISOLanguageName?.ToUpperInvariant() ?? string.Empty,
            SignatureDate = DateTime.UtcNow.Date,
            ConsentConfirmed = false,
            TermsAccepted = false
        };

        _sepaMandateEditContext = new EditContext(_sepaMandateModel);
    }

    private async Task ConfirmSepaConsentAsync()
    {
        if (_sepaMandateEditContext is null)
        {
            _snackbarService.Add(Loc["SepaMissingFields"], Severity.Warning);
            return;
        }

        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            return;
        }

        var result = await _wizardService.CreateSepaMandateAsync(userId.Value, _sepaMandateModel);

        if (!result.Success || result.MandateBytes == null || result.MandateBytes.Length == 0)
        {
            _snackbarService.Add(result.ErrorMessage ?? Loc["SepaGenerationFailed"], Severity.Error);
            return;
        }

        try
        {
            var base64String = Convert.ToBase64String(result.MandateBytes);
            _commonJsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/common.js");
            await _commonJsModule.InvokeVoidAsync("downloadFileFromBase64", base64String, $"sepa-mandate-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to trigger SEPA mandate download");
            _snackbarService.Add(Loc["SepaGenerationFailed"], Severity.Error);
            return;
        }

        _accessState = AccessState.Ready;
        _hasStoredMandate = true;
        _snackbarService.Add(Loc["SepaMandateGenerated"], Severity.Success);

        if (_pendingConfirmationAfterSepa)
        {
            _pendingConfirmationAfterSepa = false;
            await ProceedToConfirmationAsync();
        }
    }

    private void OnSepaConsentInvalid()
    {
        _sepaMandateEditContext?.Validate();
        _snackbarService.Add(Loc["SepaMissingFields"], Severity.Warning);
    }

    private void CancelAccess()
    {
        _navManager.NavigateTo(NavigationConstants.DashboardUrl);
    }

    private enum AccessState
    {
        Loading,
        Introduction,
        SepaConsent,
        Ready
    }

    private async Task ConfirmOrderAsync()
    {
        if (_currentRequest is null)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            return;
        }

        var result = await _wizardService.ConfirmOrderAsync(_currentRequest.Id, "skipped_payment");
        
        if (!result.Success)
        {
            _snackbarService.Add(result.ErrorMessage ?? Loc["ErrorTitle"], Severity.Error);
            return;
        }

        _currentRequest = result.Request;
        await LoadReportAsync(_currentRequest);

        _currentStep = 3;
        _shouldTriggerAutomaticDownload = true;
        _automaticDownloadTriggered = false;
        await EnsureReportPdfAsync(_currentRequest);
        StateHasChanged();
    }

    private string? _reportDownloadUrl;

    private async Task EnsureReportPdfAsync(CompanyCheckRequest? request, bool forceRegeneration = false)
    {
        if (request is null)
        {
            return;
        }

        if (!forceRegeneration && TryUseExistingReport(request))
        {
            await InvokeAsync(StateHasChanged);
            await TriggerAutomaticDownloadIfReadyAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(request.CreditsafeCompanyId))
        {
            _snackbarService.Add(Loc["PdfDownloadFailed"], Severity.Error);
            return;
        }

        _isGeneratingPdf = true;
        _reportDownloadUrl = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await _wizardService.GenerateReportPdfAsync(request.Id);

            if (!result.Success || result.UpdatedRequest == null)
            {
                _snackbarService.Add(result.ErrorMessage ?? Loc["PdfDownloadFailed"], Severity.Error);
                return;
            }

            _currentRequest = result.UpdatedRequest;
            _reportDownloadUrl = result.DownloadUrl;

            if (!string.IsNullOrWhiteSpace(_reportDownloadUrl))
            {
                await SendReportEmailAsync(result.UpdatedRequest);
                await TriggerAutomaticDownloadIfReadyAsync();
                _snackbarService.Add(Loc["PdfDownloadSuccess"], Severity.Success);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Add($"{Loc["PdfDownloadError"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isGeneratingPdf = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SendReportEmailAsync(CompanyCheckRequest request)
    {
        if (request is null || _lastEmailedRequestId == request.Id)
        {
            return;
        }

        var recipientEmail = await GetCurrentUserEmailAsync();
        var recipientName = await GetCurrentUserNameAsync() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var success = await _wizardService.SendReportEmailAsync(request.Id, recipientEmail, recipientName);
        if (success)
        {
            _lastEmailedRequestId = request.Id;
        }
    }

    private bool TryUseExistingReport(CompanyCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReportDownloadToken) || string.IsNullOrWhiteSpace(request.ReportPdfPath))
        {
            return false;
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, request.ReportPdfPath);
        if (!File.Exists(absolutePath))
        {
            return false;
        }

        _reportDownloadUrl = $"/company-checks/report/{request.ReportDownloadToken}";
        return true;
    }

    private async Task TriggerAutomaticDownloadIfReadyAsync()
    {
        if (_shouldTriggerAutomaticDownload && !_automaticDownloadTriggered && !string.IsNullOrWhiteSpace(_reportDownloadUrl))
        {
            _automaticDownloadTriggered = true;
            _shouldTriggerAutomaticDownload = false;
            await _jsRuntime.InvokeVoidAsync("open", _reportDownloadUrl, "_blank");
        }
    }

    private async Task LoadReportAsync(CompanyCheckRequest? request)
    {
        if (request is null)
        {
            return;
        }

        var reportDto = await _wizardService.LoadReportAsync(request.Id);

        if (reportDto != null)
        {
            _report = new CompanyCheckReport
            {
                CompanyId = reportDto.CompanyId,
                Name = reportDto.Name,
                RegistrationNumber = reportDto.RegistrationNumber,
                CountryCode = reportDto.CountryCode,
                Address = reportDto.Address,
                Status = reportDto.Status,
                Score = reportDto.Score,
                FoundedOn = reportDto.FoundedOn,
                VatNumber = reportDto.VatNumber,
                Industry = reportDto.Industry
            };
        }

        _reportReady = _report is not null;
    }

    private async Task<int?> GetCurrentUserIdAsync()
    {
        if (_currentUserId.HasValue)
        {
            return _currentUserId;
        }

        _currentUserId = await _authenticationService.GetUserIdAsync();
        return _currentUserId;
    }

    private async Task<string?> GetCurrentUserEmailAsync()
    {
        return await _authenticationService.GetUserEmailAsync();
    }

    private async Task<string?> GetCurrentUserNameAsync()
    {
        return await _authenticationService.GetUserNameAsync();
    }

    private string FormatLatestAccountsDate(string? latestAccountsDate)
    {
        if (string.IsNullOrWhiteSpace(latestAccountsDate))
        {
            return Loc["SearchResultsValueMissing"];
        }

        if (DateTimeOffset.TryParse(latestAccountsDate, out var parsedDate))
        {
            return parsedDate.ToString("d", CultureInfo.CurrentCulture);
        }

        return latestAccountsDate;
    }

    private static string? NormalizeInput(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void NavigateToHistory()
    {
        _navManager.NavigateTo("/company-checks/history");
    }

    private int GetMaxAvailableStep()
    {
        var max = 0;

        if (_searchExecuted)
        {
            max = 1;
        }

        if (_requestCreated)
        {
            max = 2;
        }

        if (_reportReady)
        {
            max = 3;
        }

        return max;
    }

    private void NavigateToStep(int step)
    {
        var maxStep = GetMaxAvailableStep();
        if (step < 0 || step > maxStep)
        {
            return;
        }

        _currentStep = step;
        StateHasChanged();
    }

    private sealed record WizardStep(string TitleKey, string Icon);
}
