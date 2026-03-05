namespace Oikos.Application.Services.TaxOffice.Models;

public class TaxOfficeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
