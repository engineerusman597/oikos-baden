using Oikos.Domain.Constants;
using Oikos.Domain.Entities.Setting;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Application.Services.Setting.Models;

namespace Oikos.Application.Services.Setting;

public class SettingService : ISettingService
{
    private readonly IAppDbContextFactory _dbFactory;

    public SettingService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardNewsSettingsDto> GetDashboardNewsSettingsAsync()
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync();
        
        var keys = new[] { DashboardSettingKeys.NewsTitle, DashboardSettingKeys.NewsSummary, DashboardSettingKeys.NewsLink };
        var settingsList = await dbContext.Settings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();

        string? GetValue(string key) => settingsList.FirstOrDefault(s => s.Key == key)?.Value;

        return new DashboardNewsSettingsDto
        {
            Title = GetValue(DashboardSettingKeys.NewsTitle),
            Summary = GetValue(DashboardSettingKeys.NewsSummary),
            Link = GetValue(DashboardSettingKeys.NewsLink)
        };
    }

    public async Task UpdateDashboardNewsSettingsAsync(DashboardNewsSettingsDto settings)
    {
        using var dbContext = await _dbFactory.CreateDbContextAsync();

        await SetSettingValueAsync(dbContext, DashboardSettingKeys.NewsTitle, settings.Title);
        await SetSettingValueAsync(dbContext, DashboardSettingKeys.NewsSummary, settings.Summary);
        await SetSettingValueAsync(dbContext, DashboardSettingKeys.NewsLink, settings.Link);

        await dbContext.SaveChangesAsync();
    }

    private async Task SetSettingValueAsync(IAppDbContext dbContext, string key, string? value)
    {
        var setting = await dbContext.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            dbContext.Settings.Add(new Domain.Entities.Setting.Setting { Key = key, Value = value ?? string.Empty });
        }
        else
        {
            setting.Value = value ?? string.Empty;
        }
    }
}
