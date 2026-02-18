using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Oikos.Application.Extensions;
using Oikos.Application.Services.CompanyCheck;
using Oikos.Common.Resources;
using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Web.Components.Pages.User_Bonix.CompanyChecks;

public partial class Success : IDisposable
{
    [Inject] private ICompanyCheckWizardService WizardService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ILogger<Success> Logger { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [SupplyParameterFromQuery]
    public int RequestId { get; set; }

    [SupplyParameterFromQuery(Name = "session_id")]
    public string? SessionId { get; set; }

    private bool _isProcessing = true;
    private bool _isSuccess = false;
    private string? _downloadUrl;
    private string? _errorMessage;
    private int _pollCount = 0;
    private bool _emailSent = false;
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Success page initialized with RequestId: {RequestId}, SessionId: {SessionId}", RequestId, SessionId);
        
        if (RequestId <= 0)
        {
            Logger.LogWarning("Invalid RequestId received: {RequestId}", RequestId);
            _errorMessage = Loc["Error_InvalidRequestId"];
            _isProcessing = false;
            return;
        }

        // Start polling loop
        _ = PollStatusAsync();
    }

    private async Task PollStatusAsync()
    {
        while (_isProcessing && _pollCount < 30 && !_disposed)
        {
            await CheckStatusAsync();
            if (!_isProcessing) break;
            
            await Task.Delay(2000);
            _pollCount++;
        }

        if (_isProcessing && !_disposed)
        {
            await StopPollingAsync(Loc["Error_PaymentTimeout"]);
        }
    }

    private async Task CheckStatusAsync()
    {
        try
        {
            var status = await WizardService.GetRequestStatusAsync(RequestId);
            
            if (status != CompanyCheckStatus.PaymentConfirmed && status != CompanyCheckStatus.Completed && !string.IsNullOrWhiteSpace(SessionId))
            {
                var synced = await WizardService.SyncPaymentStatusAsync(RequestId, SessionId);
                if (synced)
                {
                    status = await WizardService.GetRequestStatusAsync(RequestId);
                }
            }

            if (status == CompanyCheckStatus.PaymentConfirmed || status == CompanyCheckStatus.Completed)
            {
                 var reportResult = await WizardService.GenerateReportPdfAsync(RequestId);
                 
                 if (reportResult.Success)
                 {
                     if (!_emailSent)
                     {
                         _emailSent = true; // Guard against multiple sends
                         try
                         {
                             // Get email from logged-in user or fallback to CustomerEmail
                             string? recipientEmail = null;
                             string recipientName = "Kunde";
                             
                             var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                             if (authState.User.Identity?.IsAuthenticated == true)
                             {
                                 recipientEmail = authState.User.GetUserEmail();
                                 recipientName = authState.User.GetUserName();
                                 
                                 if (string.IsNullOrWhiteSpace(recipientName))
                                 {
                                     recipientName = "Kunde";
                                 }
                             }
                             
                             // Fallback to CustomerEmail if user email not found
                             if (string.IsNullOrWhiteSpace(recipientEmail) && reportResult.UpdatedRequest != null)
                             {
                                 recipientEmail = reportResult.UpdatedRequest.CustomerEmail;
                             }
                             
                             if (!string.IsNullOrWhiteSpace(recipientEmail))
                             {
                                 await WizardService.SendReportEmailAsync(RequestId, recipientEmail, recipientName);
                                 Logger.LogInformation("Email sent successfully for RequestId: {RequestId} to {Email}", RequestId, recipientEmail);
                             }
                         }
                         catch (Exception ex)
                         {
                             Logger.LogWarning(ex, "Failed to send email for RequestId: {RequestId}", RequestId);
                         }
                     }
                     
                     await StopPollingAsync(null, reportResult.DownloadUrl);
                 }
                 else
                 {
                      if (_pollCount > 25)
                      {
                         await StopPollingAsync(reportResult.ErrorMessage ?? Loc["Error_ReportGenerationFailed"]);
                      }
                      // Else continue polling for PDF generation
                 }
            }
        }
        catch (Exception ex)
        {
             // Log error
             Console.WriteLine($"Error checking status: {ex.Message}");
        }
    }
    
    private async Task StopPollingAsync(string? error, string? downloadUrl = null)
    {
        if (_disposed) return;

        await InvokeAsync(() =>
        {
            _isProcessing = false;
            if (error != null)
            {
                _errorMessage = error;
                _isSuccess = false;
            }
            else
            {
                _downloadUrl = downloadUrl;
                _isSuccess = true;
            }
            StateHasChanged();
        });
    }
    
    public void Dispose()
    {
        _disposed = true;
        _isProcessing = false; 
    }
}
