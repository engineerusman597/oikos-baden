using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.Invoice;
using Oikos.Application.Services.Invoice.Models;
using Oikos.Application.Services.Authentication;
using Oikos.Domain.Enums;

namespace Oikos.Web.Components.Pages.User.Invoices.InvoiceDetail;

public partial class InvoiceDetail
{
    [Parameter] public int InvoiceId { get; set; }
    [Parameter] public string? ReturnUrl { get; set; }

    [Inject] private IInvoiceManagementService InvoiceService { get; set; } = null!;

    private InvoiceDetailDto? _invoice;
    private List<InvoiceHistoryDto> _history = new();
    private bool _isLoading = true;
    private int? _currentUserId;

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _currentUserId = await _authenticationService.GetUserIdAsync();
        await LoadInvoiceAsync();
        _isLoading = false;
    }

    private async Task LoadInvoiceAsync()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _invoice = await InvoiceService.GetInvoiceDetailAsync(InvoiceId, culture);
        _history = _invoice?.History ?? new List<InvoiceHistoryDto>();
    }

    private void NavigateBack()
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl))
        {
            try
            {
                var decodedUrl = Uri.UnescapeDataString(ReturnUrl);
                var uri = new Uri(decodedUrl, UriKind.RelativeOrAbsolute);

                if (uri.IsAbsoluteUri)
                {
                    var currentHost = new Uri(_navManager.BaseUri).Host;
                    if (uri.Host.Equals(currentHost, StringComparison.OrdinalIgnoreCase))
                    {
                        _navManager.NavigateTo(uri.PathAndQuery);
                        return;
                    }
                }
                else
                {
                    _navManager.NavigateTo(decodedUrl);
                    return;
                }
            }
            catch
            {
                // Malformed URL, fallback
            }
        }

        _navManager.NavigateTo("/invoices");
    }

    private Color ResolveColor(string? colorName, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(colorName) && Enum.TryParse<Color>(colorName, out var parsed))
        {
            return parsed;
        }
        return fallback;
    }

    private string FormatCompany(InvoiceDetailDto invoice)
        => string.IsNullOrWhiteSpace(invoice.Company) ? Loc["TableValueUnknown"] : invoice.Company!;

    private string FormatValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? Loc["TableValueUnknown"] : value!;

    private string FormatAmount(InvoiceDetailDto invoice)
    {
        var amount = string.IsNullOrWhiteSpace(invoice.Amount) ? Loc["TableValueUnknown"] : invoice.Amount!.Trim();
        if (string.IsNullOrWhiteSpace(invoice.Currency))
        {
            return amount;
        }

        return string.IsNullOrWhiteSpace(invoice.Amount)
            ? invoice.Currency!
            : $"{amount} {invoice.Currency}";
    }

    private string FormatDate(DateTime? date)
        => date.HasValue
            ? date.Value.ToLocalTime().ToString("d", CultureInfo.CurrentUICulture)
            : Loc["TableValueUnknown"];

    private string FormatDateTime(DateTime date)
        => date.ToLocalTime().ToString("g", CultureInfo.CurrentUICulture);

    private Color GetPrimaryStatusColor(InvoicePrimaryStatus status)
    {
        return status switch
        {
            InvoicePrimaryStatus.Draft => Color.Default,
            InvoicePrimaryStatus.Submitted => Color.Info,
            InvoicePrimaryStatus.InReview => Color.Warning,
            InvoicePrimaryStatus.Inquiry => Color.Error,
            InvoicePrimaryStatus.Accepted => Color.Success,
            InvoicePrimaryStatus.Court => Color.Secondary,
            InvoicePrimaryStatus.Completed => Color.Dark,
            _ => Color.Default
        };
    }

    private static string GetFileUrl(string path)
        => $"/{path.TrimStart('/')}";
}
