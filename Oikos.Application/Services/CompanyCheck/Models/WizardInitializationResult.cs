namespace Oikos.Application.Services.CompanyCheck.Models;

public record WizardInitializationResult(
    bool IsConfigured,
    string? DefaultCountry,
    decimal? Price,
    string? Currency,
    bool HasStoredMandate
);
