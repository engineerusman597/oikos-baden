using Oikos.Domain.Entities.CompanyCheck;

namespace Oikos.Application.Services.CompanyCheck.Models;

public record OrderConfirmationResult(
    bool Success,
    string? ErrorMessage,
    CompanyCheckRequest? Request
);
