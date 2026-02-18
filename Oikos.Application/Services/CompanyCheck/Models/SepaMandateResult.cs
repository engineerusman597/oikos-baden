namespace Oikos.Application.Services.CompanyCheck.Models;

public record SepaMandateResult(
    bool Success,
    byte[]? MandateBytes,
    string? ErrorMessage
);
