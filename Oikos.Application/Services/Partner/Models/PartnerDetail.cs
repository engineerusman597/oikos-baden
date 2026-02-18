namespace Oikos.Application.Services.Partner.Models;

public record PartnerDetail
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? ContactEmail { get; init; }

    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; }

    public int CustomerCount { get; init; }
}
