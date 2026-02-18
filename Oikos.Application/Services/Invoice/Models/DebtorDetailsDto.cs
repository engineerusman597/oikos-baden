namespace Oikos.Application.Services.Invoice.Models;

public enum DebtorType
{
    Individual = 0,
    Company = 1
}

public sealed record DebtorDetailsDto(
    DebtorType DebtorType,
    string? CompanyName,
    string? ContactPerson,
    string? ContactEmail,
    string? Street,
    string? PostalCode,
    string? City,
    string? Country);
