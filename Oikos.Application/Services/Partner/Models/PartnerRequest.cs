namespace Oikos.Application.Services.Partner.Models;

public class PartnerRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }

    public string? Notes { get; set; }
}
