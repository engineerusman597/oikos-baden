namespace Oikos.Application.Services.CompanyCheck.Models;

public record CompanySearchResponse(
    bool IsConfigured,
    string? ErrorMessage,
    List<CompanySummaryDto> Results
);
