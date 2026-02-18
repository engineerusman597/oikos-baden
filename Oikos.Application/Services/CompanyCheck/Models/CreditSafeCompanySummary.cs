namespace Oikos.Application.Services.CompanyCheck.Models;

public class CreditSafeCompanySummary
{
    public string CompanyId { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? SafeNumber { get; set; }

    public string? RegNo { get; set; }

    public string? SafeNo { get; set; }

    public List<string>? TradingNames { get; set; }

    public string? RegisteredCity { get; set; }

    public string? Type { get; set; }

    public string? DateOfLatestChange { get; set; }

    public string? MatchScore { get; set; }

    public string? StatusDescription { get; set; }

    public string? PostCode { get; set; }

    public string? CountryCode { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public string? LatestAccountsDate { get; set; }

    public string? Score { get; set; }

    public string? FoundedOn { get; set; }

    public string? VatNumber { get; set; }

    public string? Industry { get; set; }

    public string? RawJson { get; set; }
}

public class CreditSafeCompanyDetails : CreditSafeCompanySummary
{
}
