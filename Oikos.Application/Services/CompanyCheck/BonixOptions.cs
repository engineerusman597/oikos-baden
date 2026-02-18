namespace Oikos.Application.Services.CompanyCheck;

public class BonixOptions
{
    public decimal? ReportPrice { get; set; }

    public string? Currency { get; set; }

    public string? CreditSafeBaseUrl { get; set; }

    public string? CreditSafeUsername { get; set; }

    public string? CreditSafePassword { get; set; }

    public string? CreditSafeDefaultCountry { get; set; }

    public bool HasCreditSafeCredentials => !string.IsNullOrWhiteSpace(CreditSafeBaseUrl)
        && !string.IsNullOrWhiteSpace(CreditSafeUsername)
        && !string.IsNullOrWhiteSpace(CreditSafePassword);
}
