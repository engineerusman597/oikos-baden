using Oikos.Domain.Enums;

namespace Oikos.Application.Services.Dashboard.Models;

public class DashboardStageSummaryDto
{
    public int StageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameDe { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SummaryDe { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public InvoicePrimaryStatus PrimaryStatus { get; set; }
    public int Count { get; set; }
}
