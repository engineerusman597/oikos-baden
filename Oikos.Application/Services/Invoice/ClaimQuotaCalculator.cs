using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Application.Services.Invoice;

public static class ClaimQuotaCalculator
{
    private static readonly ClaimQuotaRule[] Rules =
    {
        new("BASIC", "ClaimsQuotaPlanBasic", 5),
        new("PLUS", "ClaimsQuotaPlanPlus", 15),
        new("PREMIUM", "ClaimsQuotaPlanPremium", null),
    };

    public static ClaimQuotaInfo Resolve(string? certificateCode)
    {
        if (string.IsNullOrWhiteSpace(certificateCode))
        {
            return new ClaimQuotaInfo("ClaimsQuotaPlanBasic", 5);
        }

        var normalized = certificateCode.Trim().ToUpperInvariant();

        foreach (var rule in Rules)
        {
            if (normalized.Contains(rule.Match, StringComparison.Ordinal))
            {
                return new ClaimQuotaInfo(rule.PlanLocalizationKey, rule.MonthlyLimit);
            }
        }

        return new ClaimQuotaInfo("ClaimsQuotaPlanUnlimited", null);
    }

    private sealed record ClaimQuotaRule(string Match, string PlanLocalizationKey, int? MonthlyLimit);
}
