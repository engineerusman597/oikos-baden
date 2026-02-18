using Microsoft.AspNetCore.Components;

using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Common.Resources;

namespace Oikos.Web.Components.Pages.Admin.Subscription;

public partial class SubscriptionPayments
{
    [Inject] private ISubscriptionReportService SubscriptionReportService { get; set; } = null!;



    private IReadOnlyList<SubscriptionPaymentRecord> _payments = Array.Empty<SubscriptionPaymentRecord>();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        try
        {
            _payments = await SubscriptionReportService.GetPaymentsAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatPrice(long? amount, string? currency)
    {
        if (!amount.HasValue)
        {
            return string.Empty;
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? string.Empty
            : $" {currency.ToUpperInvariant()}";

        return $"{amount.Value / 100m:0.00}{normalizedCurrency}";
    }
}
