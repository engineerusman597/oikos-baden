using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Web.Components.Pages.User.CompanyChecks.Dialogs;

public partial class CompanyCheckDetailDialog
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public CompanyCheckHistoryItem Item { get; set; } = default!;

    [Parameter]
    public bool ShowPurchaseSummary { get; set; } = true;

    [Parameter]
    public bool AllowSelection { get; set; }

    [Parameter]
    public string? SelectButtonLabel { get; set; }

    private static string FormatPaidAt(DateTime? paidAt)
        => paidAt?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "-";

    private static string FormatAmount(decimal amount, string? currency)
        => string.Format(CultureInfo.CurrentCulture, "{0} {1}", amount.ToString("F2", CultureInfo.CurrentCulture), (currency ?? "EUR").ToUpperInvariant());

    private string FormatDetailValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? Loc["SearchResultsValueMissing"] : value;

    private static string? ResolveDetailValue(
        CompanyCheckHistoryItem item,
        Func<CompanyCheckReport, string?> reportSelector,
        Func<CreditSafeCompanySummary, string?> summarySelector,
        string? fallback = null)
    {
        if (item.Report is not null)
        {
            var reportValue = reportSelector(item.Report);
            if (!string.IsNullOrWhiteSpace(reportValue))
            {
                return reportValue;
            }
        }

        if (item.SelectedCompany is not null)
        {
            var summaryValue = summarySelector(item.SelectedCompany);
            if (!string.IsNullOrWhiteSpace(summaryValue))
            {
                return summaryValue;
            }
        }

        return fallback;
    }

    private string GetSelectButtonLabel()
        => string.IsNullOrWhiteSpace(SelectButtonLabel) ? Loc["SearchResultsSelectButton"] : SelectButtonLabel!;

    private Color GetCloseButtonColor() => AllowSelection ? Color.Default : Color.Primary;

    private void ConfirmSelection()
    {
        if (Item is null)
        {
            return;
        }

        MudDialog?.Close(DialogResult.Ok(Item));
    }

    private void CloseDialog()
    {
        MudDialog?.Close();
    }
}
