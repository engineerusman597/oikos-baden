namespace Oikos.Web.Components.Pages.User.Models;

public sealed class StatusSummary(
    int StageId,
    string Name,
    string Description,
    string Icon,
    string TargetUri,
    string CssClass,
    string Style)
{
    public int StageId { get; } = StageId;
    public string Name { get; } = Name;
    public string Description { get; } = Description;
    public string Icon { get; } = Icon;
    public string TargetUri { get; } = TargetUri;
    public string CssClass { get; } = CssClass;
    public string Style { get; } = Style;
    public int Count { get; set; }
}

public sealed record InvoiceStageCount(int Key, int Count);

public sealed class DashboardNewsContent(string? Title, string? Summary, string? Link)
{
    public string? Title { get; } = Title;

    public string? Summary { get; } = Summary;

    public string? Link { get; } = Link;

    public bool HasLink => !string.IsNullOrWhiteSpace(Link);
}
