namespace Oikos.Application.Services.CompanyCheck.Models;

public record CompanySummaryDto(
    string CompanyId,
    string Name,
    string? CountryCode,
    string? RegistrationNumber,
    string? Address,
    string? Status,
    string? Type,
    string? LatestAccountsDate
);
