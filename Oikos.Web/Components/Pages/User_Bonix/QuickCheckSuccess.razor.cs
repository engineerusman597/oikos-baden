using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Oikos.Common.Resources;
using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Web.Components.Pages.User_Bonix;

public partial class QuickCheckSuccess : IDisposable
{
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

    protected override async Task OnInitializedAsync()
    {
        if (RequestId <= 0)
        {
            _errorMessage = Loc["Error_InvalidRequestId"];
            _isProcessing = false;
            return;
        }

        // Start polling loop
        _ = PollStatusAsync();
    }

    private async Task PollStatusAsync()
    {
        while (_isProcessing && _pollCount < 30)
        {
            await CheckStatusAsync();
            if (!_isProcessing) break;
            
            await Task.Delay(2000);
            _pollCount++;
        }

        if (_isProcessing)
        {
            await StopPollingAsync(Loc["Error_PaymentTimeout"]);
        }
    }

    private async Task CheckStatusAsync()
    {
        try
        {
            var status = await _wizardService.GetRequestStatusAsync(RequestId);
            
            // If strictly polling, we might want to try to sync with Stripe if not confirmed yet
            if (status != CompanyCheckStatus.PaymentConfirmed && status != CompanyCheckStatus.Completed && !string.IsNullOrWhiteSpace(SessionId))
            {
                var synced = await _wizardService.SyncPaymentStatusAsync(RequestId, SessionId);
                if (synced)
                {
                    status = await _wizardService.GetRequestStatusAsync(RequestId);
                }
            }

            if (status == CompanyCheckStatus.PaymentConfirmed || status == CompanyCheckStatus.Completed)
            {
                 var reportResult = await _wizardService.GenerateReportPdfAsync(RequestId);
                 if (reportResult.Success)
                 {
                     if (reportResult.UpdatedRequest != null && !string.IsNullOrWhiteSpace(reportResult.UpdatedRequest.CustomerEmail) && !_emailSent)
                     {
                         _emailSent = true; // Guard against multiple sends
                         try
                         {
                            await _wizardService.SendReportEmailAsync(RequestId, reportResult.UpdatedRequest.CustomerEmail, "Kunde");
                         }
                         catch { /* ignore email error for UI */ }
                     }
                     
                     await StopPollingAsync(null, reportResult.DownloadUrl);
                 }
                 else
                 {
                      if (_pollCount > 25)
                      {
                         await StopPollingAsync(reportResult.ErrorMessage ?? Loc["Error_ReportGenerationFailed"]);
                      }
                 }
            }
        }
        catch (Exception ex)
        {
             // Log?
        }
    }
    
    private async Task StopPollingAsync(string? error, string? downloadUrl = null)
    {
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
        _isProcessing = false; // Stop loop if navigated away
    }
}
