namespace Oikos.Application.Services.Partner.Models;

public sealed record PartnerCommissionDto(
    decimal Amount,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Status);
