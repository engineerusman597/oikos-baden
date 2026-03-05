namespace Oikos.Application.Services.TaxOffice.Models;

public sealed record TaxOfficeLicenseDto(
    int UserId,
    string Name,
    string? CustomerNumber,
    string? Email,
    string PlanName,
    bool IsActive,
    DateTime? ExpirationDate,
    string BillingInterval);
