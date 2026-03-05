namespace Oikos.Application.Services.TaxOffice.Models;

public sealed record TaxOfficeDetail(
    int Id,
    string Name,
    string? BusinessName,
    string? Email,
    string? PhoneNumber,
    string? ContactPerson,
    string? Address,
    string? PostalCode,
    string? City,
    string? Notes,
    string Code,
    bool IsActive,
    DateTime CreatedAt);
