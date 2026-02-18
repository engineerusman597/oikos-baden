using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Dashboard.Models;
using Oikos.Domain.Constants; // Check if needed, usually effectively included via global? No, specific file.

namespace Oikos.Application.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContextFactory _dbFactory;

    public DashboardService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<DashboardStatusSummaryDto>> GetDashboardStatusSummariesAsync(int userId, bool isAdmin)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        var query = context.Invoices.AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(i => i.UserId == userId);
        }

        var results = await query
            .GroupBy(i => i.PrimaryStatus)
            .Select(g => new DashboardStatusSummaryDto(g.Key, g.Count()))
            .ToListAsync();

        return results;
    }
}
