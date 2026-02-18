namespace Oikos.Application.Services.CompanyCheck.Models;

public record CreateOrderRequest(
    int? UserId,
    string RequestedCompanyName,
    string CompanyId,
    string CompanyName,
    string? CountryCode,
    string? RegistrationNumber,
    decimal Price,
    string Currency,
    string? UserEmail,
    string? SelectedCompanyJson,
    string? SearchReason
);
