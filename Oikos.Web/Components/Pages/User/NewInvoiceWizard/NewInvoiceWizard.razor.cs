using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Application.Services.Subscription;
using Oikos.Common.Helpers;
using Oikos.Web.Constants;
using Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models;

using DebtorType = Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models.DebtorType;
using ClaimStartOption = Oikos.Web.Components.Pages.User.NewInvoiceWizard.Models.ClaimStartOption;

namespace Oikos.Web.Components.Pages.User.NewInvoiceWizard;

public partial class NewInvoiceWizard
{
    private static long MaxUploadSizeBytes => UploadConstants.MaxUploadSizeBytes;
    private static string TermsUrl => ExternalUrlConstants.TermsUrl;
    private static string PrivacyUrl => ExternalUrlConstants.PrivacyUrl;
    private static string DashboardUrl => NavigationConstants.DashboardUrl;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ILogger<NewInvoiceWizard> Logger { get; set; } = null!;

    [Inject] private IInvoiceSubmissionService InvoiceSubmissionService { get; set; } = null!;
    [Inject] private ISubscriptionPlanService _subscriptionPlanService { get; set; } = null!;


    private readonly WizardStep[] _steps =
    {
        new("WizardStep1Title", "WizardStep1Description", Icons.Material.Filled.UploadFile),
        new("WizardStep3Title", "WizardStep3Description", Icons.Material.Filled.Badge),
        new("WizardStep4Title", "WizardStep4Description", Icons.Material.Filled.FactCheck),
        new("WizardStep2Title", "WizardStep2Description", Icons.Material.Filled.Gavel)
    };

    private readonly string[] _currencies = { "EUR" };

    private readonly List<InvoiceDraft> _invoiceDrafts = new();

    private DebtorDetailsModel _debtor = new();
    private PowerOfAttorneyModel _powerOfAttorney = new();
    private ClaimPreferencesModel _preferences = new();
    private MudForm? _powerOfAttorneyForm;
    private MudForm? _debtorForm;
    private MudForm? _finalForm;
    private int _currentStep;
    private bool _isSubmitting;
    private bool _isUploading;
    private bool _uploadCompleted;
    private double _uploadProgress;
    private MudFileUpload<IBrowserFile>? _fileUpload;
    private string _currentUserName = string.Empty;
    private int? _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (!await EnsureActiveSubscriptionAsync())
        {
            return;
        }

        await LoadUserAsync();
    }

    private async Task LoadUserAsync()
    {
        _currentUserName = await _authenticationService.GetUserNameAsync();
        if (string.IsNullOrWhiteSpace(_currentUserName))
        {
            _currentUserName = Loc["WizardSummaryDefaultName"];
        }
    }

    private async Task<bool> EnsureActiveSubscriptionAsync()
    {
        var accessResult = await SubscriptionAccessHelper.EvaluateAccessAsync(
            _authenticationService,
            _subscriptionPlanService,
            Logger);

        if (!accessResult.HasUser || !accessResult.HasActiveSubscription)
        {
            _snackbarService.Add(Loc["SubscriptionRequiredMessage"], Severity.Warning);
            NavigationManager.NavigateTo(DashboardUrl, true);
            return false;
        }

        _currentUserId = accessResult.UserId;
        return true;
    }

    private async Task HandleFileUploadChanged(InputFileChangeEventArgs args)
    {
        if (args is null)
        {
            return;
        }

        var file = args.File ?? args.GetMultipleFiles(1).FirstOrDefault();
        await OnFilesSelected(file);
    }

    private async Task OnFilesSelected(IBrowserFile file)
    {
        if (_isUploading)
        {
            return;
        }

        if (file is null)
        {
            _snackbarService.Add(Loc["WizardUploadEmpty"], Severity.Info);
            await ResetFilePickerAsync();
            return;
        }

        if (!IsPdfFile(file))
        {
            _snackbarService.Add(Loc["UploadPdfOnly"], Severity.Warning);
            await ResetFilePickerAsync();
            return;
        }

        if (file.Size > MaxUploadSizeBytes)
        {
            _snackbarService.Add(Loc["UploadFileTooLarge"], Severity.Warning);
            await ResetFilePickerAsync();
            return;
        }

        var userId = _currentUserId ??= await _authenticationService.GetUserIdAsync();

        foreach (var existingDraft in _invoiceDrafts.ToList())
        {
            FileHelper.TryDeleteFile(existingDraft.TempFilePath);
        }

        _invoiceDrafts.Clear();
        _debtor = new DebtorDetailsModel();
        _powerOfAttorney = new PowerOfAttorneyModel();
        _isUploading = true;
        _uploadCompleted = false;
        _uploadProgress = 0.05;
        await RefreshAsync();

        try
        {
            using var fileStream = file.OpenReadStream(MaxUploadSizeBytes);
            var request = new FileUploadRequest(fileStream, file.Name, file.Size, userId);

            _uploadProgress = 0.5;
            await RefreshAsync();

            var result = await InvoiceSubmissionService.ProcessFileUploadAsync(request);

            if (!result.Success || result.Draft is null)
            {
                throw new Exception(result.ErrorMessageKey);
            }

            _uploadProgress = 1.0;
            await RefreshAsync();

            var draftDto = result.Draft;
            var draft = new InvoiceDraft(file, draftDto.TempFilePath, draftDto.WebPath);
            draft.Details.InvoiceNumber = draftDto.Details.InvoiceNumber ?? string.Empty;
            draft.Details.Amount = draftDto.Details.Amount ?? string.Empty;
            draft.Details.Currency = draftDto.Details.Currency ?? "EUR";
            draft.Details.InvoiceDate = draftDto.Details.InvoiceDate;
            draft.Details.Description = draftDto.Details.Description;

            if (result.Extraction is not null)
            {
                ApplyDebtorExtraction(result.Extraction);
            }

            _invoiceDrafts.Add(draft);
        }
        catch
        {
            _snackbarService.Add(Loc["UploadError"], Severity.Error);
            _invoiceDrafts.Clear();
            _isUploading = false;
            _uploadCompleted = false;
            _uploadProgress = 0;
            _currentStep = 0;
            _powerOfAttorney = new PowerOfAttorneyModel();
            await RefreshAsync();
            await ResetFilePickerAsync();
            return;
        }

        _isUploading = false;
        _uploadCompleted = true;
        _currentStep = 1;

        await RefreshAsync();
        await ResetFilePickerAsync();
    }

    private bool IsPdfFile(IBrowserFile file)
    {
        if (string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(file.Name);
        return !string.IsNullOrWhiteSpace(extension) && string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ResetFilePickerAsync()
    {
        if (_fileUpload is not null)
        {
            try
            {
                await _fileUpload.ClearAsync();
            }
            catch
            {
                // ignored
            }
        }

        await RefreshAsync();
    }

    private void RemoveInvoice(Guid id)
    {
        var draft = _invoiceDrafts.FirstOrDefault(d => d.Id == id);
        if (draft is null)
        {
            return;
        }

        _invoiceDrafts.Remove(draft);
        FileHelper.TryDeleteFile(draft.TempFilePath);

        if (!_invoiceDrafts.Any())
        {
            _currentStep = 0;
            _uploadCompleted = false;
            _uploadProgress = 0;
            _powerOfAttorney = new PowerOfAttorneyModel();
        }
        else
        {
            _uploadCompleted = true;
            _uploadProgress = 1;
        }

        _ = ResetFilePickerAsync();
    }


    private Task RefreshAsync() => InvokeAsync(StateHasChanged);

    private string GetUploadStatusIconClass()
    {
        return _uploadCompleted
            ? "wizard-upload-status-icon"
            : "wizard-upload-status-icon spin";
    }

    private string FormatFileSize(long size)
    {
        if (size <= 0)
        {
            return "0 KB";
        }

        var suffixes = new[] { "KB", "MB", "GB" };
        double displaySize = size / 1024d;
        var suffixIndex = 0;

        while (displaySize >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            displaySize /= 1024;
            suffixIndex++;
        }

        return string.Format(CultureInfo.CurrentUICulture, "{0:0.##} {1}", displaySize, suffixes[suffixIndex]);
    }

    private string FormatTotalAmount()
    {
        var values = _invoiceDrafts
            .Select(d => BuildAmountDisplay(d.Details.Amount, d.Details.Currency))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();

        if (values.Count == 0)
        {
            return Loc["WizardInvoiceSummaryEmpty"];
        }

        return values.Count == 1
            ? values[0]
            : string.Join(" + ", values);
    }

    private static string? BuildAmountDisplay(string? amount, string? currency)
    {
        if (string.IsNullOrWhiteSpace(amount))
        {
            return null;
        }

        var trimmedAmount = amount.Trim();

        if (string.IsNullOrWhiteSpace(currency))
        {
            return trimmedAmount;
        }

        var trimmedCurrency = currency.Trim();
        return string.IsNullOrWhiteSpace(trimmedCurrency)
            ? trimmedAmount
            : $"{trimmedAmount} {trimmedCurrency}";
    }

    private bool CanProceed()
    {
        return _currentStep switch
        {
            0 => _invoiceDrafts.Any() && !_isUploading,
            1 => _invoiceDrafts.Any(),
            2 => _preferences.AgreeTerms && _preferences.AgreePrivacy && IsStartOptionValid(),
            3 => _powerOfAttorney.Accepted && !string.IsNullOrWhiteSpace(_powerOfAttorney.Signature),
            _ => false
        };
    }

    private async Task GoNextAsync()
    {
        if (_currentStep == 0)
        {
            if (_isUploading)
            {
                _snackbarService.Add(Loc["WizardUploadProcessingWait"], Severity.Info);
                return;
            }

            if (!_invoiceDrafts.Any())
            {
                _snackbarService.Add(Loc["WizardUploadEmptyError"], Severity.Warning);
                return;
            }

            _currentStep++;
            return;
        }

        if (_currentStep == 1)
        {
            if (_debtorForm is not null)
            {
                await _debtorForm.Validate();
            }

            var invoicesValid = ValidateInvoiceDrafts();

            if (_debtorForm is not null && !_debtorForm.IsValid || !invoicesValid)
            {
                _snackbarService.Add(Loc["WizardValidationErrors"], Severity.Warning);
                return;
            }

            _currentStep++;
            return;
        }

        if (_currentStep == 2)
        {
            if (!_preferences.AgreeTerms || !_preferences.AgreePrivacy)
            {
                _snackbarService.Add(Loc["WizardLegalValidation"], Severity.Warning);
                return;
            }

            if (!IsStartOptionValid())
            {
                _snackbarService.Add(Loc["WizardStartOptionAfterReminderValidation"], Severity.Warning);
                return;
            }

            _currentStep++;
            return;
        }

        if (_currentStep == 3)
        {
            if (_powerOfAttorneyForm is not null)
            {
                await _powerOfAttorneyForm.Validate();
            }

            if (string.IsNullOrWhiteSpace(_powerOfAttorney.Signature) || !_powerOfAttorney.Accepted ||
                _powerOfAttorneyForm is not null && !_powerOfAttorneyForm.IsValid)
            {
                _snackbarService.Add(Loc["WizardPowerOfAttorneyValidation"], Severity.Warning);
                return;
            }

            _powerOfAttorney.SignedAt = DateTimeOffset.UtcNow;
            await SubmitAsync();
        }
    }

    private bool IsStartOptionValid()
    {
        if (_preferences.StartOption != ClaimStartOption.AfterReminder)
        {
            return true;
        }

        return _preferences.StartAfterReminderAt is not null &&
               _preferences.StartAfterReminderAt.Value.Date >= DateTime.Today;
    }

    private Task OnStartOptionChanged(ClaimStartOption option)
    {
        _preferences.StartOption = option;

        if (option != ClaimStartOption.AfterReminder)
        {
            _preferences.StartAfterReminderAt = null;
        }
        else if (_preferences.StartAfterReminderAt is null || _preferences.StartAfterReminderAt.Value.Date < DateTime.Today)
        {
            _preferences.StartAfterReminderAt = DateTime.Today;
        }

        return InvokeAsync(StateHasChanged);
    }

    private Task OnDebtorTypeChanged(DebtorType debtorType)
    {
        _debtor.DebtorType = debtorType;

        if (debtorType == DebtorType.Individual)
        {
            _debtor.ContactEmail = null;
        }

        _debtorForm?.ResetValidation();
        return InvokeAsync(StateHasChanged);
    }

    private string? ValidateContactEmail(string? value)
    {
        if (_debtor.DebtorType == DebtorType.Individual && string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return _debtor.DebtorType == DebtorType.Company
                ? Loc["WizardContactEmailRequired"]
                : null;
        }

        var attribute = new EmailAddressAttribute();
        return attribute.IsValid(value) ? null : Loc["WizardContactEmailInvalid"];
    }

    private void NavigateToStep(int step)
    {
        if (step < 0 || step >= _steps.Length)
        {
            return;
        }

        if (step > _currentStep)
        {
            return;
        }

        _currentStep = step;
        StateHasChanged();
    }

    private void CancelWizard()
    {
        foreach (var draft in _invoiceDrafts.ToList())
        {
            FileHelper.TryDeleteFile(draft.TempFilePath);
        }

        NavigationManager.NavigateTo("/invoices");
    }

    private bool ValidateInvoiceDrafts()
    {
        var valid = true;
        foreach (var draft in _invoiceDrafts)
        {
            if (string.IsNullOrWhiteSpace(draft.Details.InvoiceNumber) ||
                string.IsNullOrWhiteSpace(draft.Details.Amount) ||
                string.IsNullOrWhiteSpace(draft.Details.Currency) ||
                !draft.Details.InvoiceDate.HasValue ||
                draft.Details.InvoiceDate.Value.Date > DateTime.Today)
            {
                valid = false;
            }
        }

        return valid;
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
            var userId = _currentUserId ??= await _authenticationService.GetUserIdAsync();
            var userName = _currentUserName;
            var userEmail = await _authenticationService.GetUserEmailAsync();

            var debtorDto = new DebtorDetailsDto(
                _debtor.DebtorType == DebtorType.Company ? Application.Services.Invoice.Models.DebtorType.Company : Application.Services.Invoice.Models.DebtorType.Individual,
                _debtor.CompanyName,
                _debtor.ContactName,
                _debtor.ContactEmail,
                _debtor.Street,
                _debtor.PostalCode,
                _debtor.City,
                null);

            var poaDto = new PowerOfAttorneyDto(
                _currentUserName,
                string.Empty,
                string.Empty,
                string.Empty,
                _powerOfAttorney.Signature,
                _powerOfAttorney.SignedAt ?? DateTimeOffset.UtcNow,
                _powerOfAttorney.Accepted);

            var prefDto = new ClaimPreferencesDto(
                _preferences.StartOption == ClaimStartOption.Immediate ? Application.Services.Invoice.Models.ClaimStartOption.Immediately : Application.Services.Invoice.Models.ClaimStartOption.AfterReminder,
                _preferences.StartAfterReminderAt,
                _preferences.AgreeTerms,
                _preferences.AgreePrivacy);

            var drafts = _invoiceDrafts.Select(d => new InvoiceDraftDto(
                d.Id,
                d.TempFilePath,
                d.PreviewUrl,
                d.FileName,
                d.Size,
                new InvoiceDetailsDto(
                    d.Details.InvoiceNumber,
                    d.Details.Amount,
                    d.Details.Currency,
                    d.Details.InvoiceDate,
                    BuildDescription(d) // Use local description builder to map to DTO
                ))).ToList();

            var request = new InvoiceSubmissionRequest(
                drafts,
                debtorDto,
                poaDto,
                prefDto,
                userId,
                userName,
                userEmail);

            var result = await InvoiceSubmissionService.SubmitInvoicesAsync(request);

            if (!result.Success)
            {
                _isSubmitting = false;
                var error = result.ErrorMessageKey ?? "UploadError";
                _snackbarService.Add(Loc[error], Severity.Error);
                return;
            }

            _snackbarService.Add(Loc["WizardSuccessMessage"], Severity.Success);
            _isSubmitting = false;
            NavigationManager.NavigateTo("/invoices");
        }
        catch
        {
            _isSubmitting = false;
            _snackbarService.Add(Loc["UploadError"], Severity.Error);
        }
    }

    private void ApplyDebtorExtraction(InvoiceAiExtractionResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.DebtorCompany))
        {
            _debtor.CompanyName = result.DebtorCompany;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorStreet))
        {
            _debtor.Street = result.DebtorStreet;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorPostalCode))
        {
            _debtor.PostalCode = result.DebtorPostalCode;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorCity))
        {
            _debtor.City = result.DebtorCity;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorContactName))
        {
            _debtor.ContactName = result.DebtorContactName;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorContactEmail))
        {
            _debtor.ContactEmail = result.DebtorContactEmail;
        }

        if (!string.IsNullOrWhiteSpace(result.DebtorContactPhone))
        {
            _debtor.ContactPhone = result.DebtorContactPhone;
        }
    }

    private string BuildDescription(InvoiceDraft draft)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Format(Loc["WizardDescriptionInvoiceNumber"], draft.Details.InvoiceNumber));

        if (!string.IsNullOrWhiteSpace(_powerOfAttorney.Signature) && _powerOfAttorney.SignedAt.HasValue)
        {
            var signedAt = _powerOfAttorney.SignedAt.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);
            builder.AppendLine(string.Format(Loc["WizardDescriptionPowerOfAttorneySignature"], _powerOfAttorney.Signature, signedAt));
        }

        if (!string.IsNullOrWhiteSpace(_debtor.ContactName))
        {
            builder.AppendLine(string.Format(Loc["WizardDescriptionContact"], _debtor.ContactName));
        }

        if (!string.IsNullOrWhiteSpace(draft.Details.VatAmount))
        {
            builder.AppendLine(string.Format(Loc["WizardDescriptionVat"], BuildAmountDisplay(draft.Details.VatAmount, draft.Details.Currency)));
        }

        if (!string.IsNullOrWhiteSpace(_preferences.AdditionalNotes))
        {
            builder.AppendLine(string.Format(Loc["WizardDescriptionNotes"], _preferences.AdditionalNotes));
        }

        if (_preferences.StartOption == ClaimStartOption.AfterReminder && _preferences.StartAfterReminderAt.HasValue)
        {
            var startAt = _preferences.StartAfterReminderAt.Value.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);
            builder.AppendLine(string.Format(Loc["WizardDescriptionStartScheduled"], startAt));
        }
        else
        {
            builder.AppendLine(string.Format(Loc["WizardDescriptionStart"], Loc[$"WizardStartOption{_preferences.StartOption}"]));
        }

        return builder.ToString();
    }


}
