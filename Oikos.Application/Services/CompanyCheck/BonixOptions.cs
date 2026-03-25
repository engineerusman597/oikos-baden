namespace Oikos.Application.Services.CompanyCheck;

public class BonixOptions
{
    public decimal? ReportPrice { get; set; }

    public string? Currency { get; set; }

    public string? CreditSafeBaseUrl { get; set; }

    public string? CreditSafeUsername { get; set; }

    public string? CreditSafePassword { get; set; }

    public string? CreditSafeDefaultCountry { get; set; }

    /// <summary>
    /// Optional absolute path for storing uploaded files (e.g. /var/www/rechtfix/uploads).
    /// When set, this path is used instead of auto-detecting a writable path under wwwroot.
    /// </summary>
    public string? StoragePath { get; set; }

    public bool HasCreditSafeCredentials => !string.IsNullOrWhiteSpace(CreditSafeBaseUrl)
        && !string.IsNullOrWhiteSpace(CreditSafeUsername)
        && !string.IsNullOrWhiteSpace(CreditSafePassword);
}
