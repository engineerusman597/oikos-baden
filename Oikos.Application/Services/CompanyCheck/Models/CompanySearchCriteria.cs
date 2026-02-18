using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Oikos.Application.Services.CompanyCheck.Models;

public class CompanySearchCriteria : IValidatableObject
{
    [StringLength(200)]
    public string? CompanyName { get; set; } = string.Empty;

    [StringLength(2, MinimumLength = 2)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? RegistrationNumber { get; set; }

    [StringLength(100)]
    public string? VatNumber { get; set; }

    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? PostCode { get; set; }

    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(20)]
    public string? CompanyType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasName = !string.IsNullOrWhiteSpace(CompanyName);
        var hasRegistration = !string.IsNullOrWhiteSpace(RegistrationNumber);
        var hasVat = !string.IsNullOrWhiteSpace(VatNumber);
        var hasAddress = !string.IsNullOrWhiteSpace(Street)
                         || !string.IsNullOrWhiteSpace(City)
                         || !string.IsNullOrWhiteSpace(PostCode);
        var hasPhone = !string.IsNullOrWhiteSpace(PhoneNumber);
        var hasStatus = !string.IsNullOrWhiteSpace(Status);

        if (hasName && CompanyName!.Length < 2)
        {
            yield return new ValidationResult(
                "The field CompanyName must be a string with a minimum length of 2 and a maximum length of 200.",
                new[] { nameof(CompanyName) });
        }

        if (!hasName && !hasRegistration && !hasVat && !hasAddress && !hasPhone && !hasStatus)
        {
            yield return new ValidationResult(
                "Please enter at least one search field (e.g., company name, registration number, or another filter).",
                new[] { nameof(CompanyName) });
        }
    }
}
