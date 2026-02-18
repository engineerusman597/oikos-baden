namespace Oikos.Application.Services.CompanyCheck.Models;

public record CompanySearchRequest(
    string? CompanyName,
    string? Country,
    string? RegistrationNumber,
    string? VatNumber,
    string? Street,
    string? City,
    string? PostCode,
    string? PhoneNumber,
    string? Status,
    string? CompanyType
);
