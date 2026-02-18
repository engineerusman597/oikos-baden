namespace Oikos.Application.Services.Invoice.Models;

public sealed record PowerOfAttorneyDto(
    string Name,
    string Street,
    string PostalCode,
    string City,
    string Signature,
    DateTimeOffset SignedAt,
    bool Accepted);
