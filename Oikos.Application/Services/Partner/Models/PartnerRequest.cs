namespace Oikos.Application.Services.Partner.Models;

public class PartnerRequest
{
    public string Name { get; set; } = string.Empty;

    public string? PartnerType { get; set; }

    public string? BusinessName { get; set; }

    public string? Code { get; set; }

    public string? ContactEmail { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ContactPerson { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public string? CommissionType { get; set; }

    public decimal? CommissionRate { get; set; }

    public int? CommissionPeriodMonths { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Password { get; set; }
}
