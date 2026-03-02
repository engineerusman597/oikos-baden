namespace Oikos.Application.Services.Partner.Models;

public sealed record PartnerSubPartnerDto(
    int PartnerId,
    string Name,
    string? Notes,
    string? ContactEmail,
    string Code,
    bool IsActive,
    DateTime CreatedAt);
