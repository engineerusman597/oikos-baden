namespace Oikos.Application.Services.Partner.Models;

public record PartnerDetail
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? PartnerType { get; init; }

    public string? BusinessName { get; init; }

    public string Code { get; init; } = string.Empty;

    public string? ContactEmail { get; init; }

    public string? PhoneNumber { get; init; }

    public string? ContactPerson { get; init; }

    public string? Address { get; init; }

    public string? Notes { get; init; }

    public string? CommissionType { get; init; }

    public decimal? CommissionRate { get; init; }

    public int? CommissionPeriodMonths { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public int CustomerCount { get; init; }
}
