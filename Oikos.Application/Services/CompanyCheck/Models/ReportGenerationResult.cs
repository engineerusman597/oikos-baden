using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Application.Services.CompanyCheck.Models;

public record ReportGenerationResult(
    bool Success,
    string? ErrorMessage,
    string? DownloadUrl,
    CompanyCheckRequest? UpdatedRequest
);
