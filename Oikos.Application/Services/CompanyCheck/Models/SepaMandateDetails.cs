using System;
using System.ComponentModel.DataAnnotations;

namespace Oikos.Application.Services.CompanyCheck.Models;

public class SepaMandateDetails
{
    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LegalForm { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AuthorizedRepresentative { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AccountHolderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(34)]
    public string Iban { get; set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string Bic { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PaymentPurpose { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PaymentInterval { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SignatureCity { get; set; } = string.Empty;

    [Required]
    public DateTime? SignatureDate { get; set; } = DateTime.UtcNow.Date;

    public string CreditorName { get; set; } = "Oikos Holding GmbH – Marke Rechtfix";

    public bool ConsentConfirmed { get; set; }

    public bool TermsAccepted { get; set; }
}
