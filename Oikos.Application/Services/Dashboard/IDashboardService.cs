using Oikos.Application.Services.Dashboard.Models;

namespace Oikos.Application.Services.Dashboard;

public interface IDashboardService
{
    Task<List<DashboardStatusSummaryDto>> GetDashboardStatusSummariesAsync(int userId, bool isAdmin);
    Task<List<DashboardRecentActivityDto>> GetRecentActivitiesAsync(int userId, int count);
}
