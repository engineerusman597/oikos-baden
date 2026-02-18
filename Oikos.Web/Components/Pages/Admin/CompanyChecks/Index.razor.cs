using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;
using Oikos.Domain.Entities.CompanyCheck;
using System.Globalization;
using Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs;
using Oikos.Web.Components.Shared.Dialogs;

namespace Oikos.Web.Components.Pages.Admin.CompanyChecks;

public partial class Index
{
    [Inject] private ILogger<Index> Logger { get; set; } = null!;

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

    private string? _errorMessage;
    private string? _searchReason;

    private int _currentStep;
    private bool _shouldTriggerAutomaticDownload;
    private bool _automaticDownloadTriggered;
    private readonly string[] _companyStatuses = { "Active", "NonActive", "Pending", "Other" };
    private readonly string[] _companyTypes = { "Ltd", "NonLtd" };

    private int? _lastEmailedRequestId;
    private int? _adminUserId;

    protected override async Task OnInitializedAsync()
    {
        _adminUserId = await GetCurrentUserIdAsync();
        var initResult = await _wizardService.InitializeAsync(_adminUserId); // Pass admin's userId
        _creditsafeConfigured = initResult.IsConfigured;
        _searchModel.Country ??= initResult.DefaultCountry;
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

        try
        {
            var requestedName = string.IsNullOrWhiteSpace(_searchModel.CompanyName)
                ? _selectedCompany.Name
                : _searchModel.CompanyName;

            var orderRequest = new Oikos.Application.Services.CompanyCheck.Models.CreateOrderRequest(
                _adminUserId, // UserId - admin's userId
                requestedName,
                _selectedCompany.CompanyId ?? string.Empty,
                _selectedCompany.Name ?? string.Empty,
                _selectedCompany.CountryCode,
                _selectedCompany.RegistrationNumber,
                0m, // Price - 0 for admin
                "EUR",
                null, // Email - null for admin
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

    private async Task ConfirmOrderAsync()
    {
        if (_currentRequest is null)
        {
            _snackbarService.Add(Loc["ErrorTitle"], Severity.Error);
            return;
        }

        // Admin: skip payment, directly confirm
        var result = await _wizardService.ConfirmOrderAsync(_currentRequest.Id, "admin_direct_access");

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

    private void NavigateToHistory()
    {
        _navManager.NavigateTo("/admin/company-checks/history");
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

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var userId = await _authenticationService.GetUserIdAsync();
        return userId;
    }

    private sealed record WizardStep(string TitleKey, string Icon);
}
