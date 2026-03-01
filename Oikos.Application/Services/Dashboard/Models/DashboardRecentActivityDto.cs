namespace Oikos.Application.Services.Dashboard.Models;

public sealed record DashboardRecentActivityDto(
    string StageName,
    string? Note,
    DateTime ChangedAt);
