namespace Oikos.Application.Services.CompanyCheck.Models;

public record CompanyReportDto(
    string? CompanyId,
    string? Name,
    string? RegistrationNumber,
    string? CountryCode,
    string? Address,
    string? Status,
    string? Score,
    string? FoundedOn,
    string? VatNumber,
    string? Industry
);
