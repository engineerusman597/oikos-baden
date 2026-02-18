using Oikos.Application.Services.Setting.Models;

namespace Oikos.Application.Services.Setting;

public interface ISettingService
{
    Task<DashboardNewsSettingsDto> GetDashboardNewsSettingsAsync();

    Task UpdateDashboardNewsSettingsAsync(DashboardNewsSettingsDto settings);
}
