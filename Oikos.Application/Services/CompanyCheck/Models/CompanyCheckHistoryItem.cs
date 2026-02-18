using System;
using Oikos.Application.Services.CompanyCheck.Models;

namespace Oikos.Application.Services.CompanyCheck.Models;

public class CompanyCheckHistoryItem
{
    public int RequestId { get; set; }

    public string? CompanyName { get; set; }

    public string? CountryCode { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? Address { get; set; }

    public string? Username { get; set; }

    public DateTime? PaidAt { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public CompanyCheckReport? Report { get; set; }

    public CreditSafeCompanySummary? SelectedCompany { get; set; }

    public string? ReportJson { get; set; }

    public string? DownloadToken { get; set; }

    public string? DownloadUrl => string.IsNullOrWhiteSpace(DownloadToken)
        ? null
        : $"/company-checks/report/{DownloadToken}";
}
