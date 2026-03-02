namespace Oikos.Application.Services.Partner.Models;

public sealed record CreateSubPartnerRequest(
    string Name,
    string? Business,
    string Email,
    string? PhoneNumber,
    string CommissionType,
    decimal CommissionRate,
    string Password);
