using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;
using Oikos.Domain.Entities.Setting;

namespace Oikos.Application.Services.User;

public class UserSettingService : IUserSettingService
{
    private readonly IAppDbContextFactory _dbFactory;

    public UserSettingService(IAppDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string?> GetSettingAsync(int userId, string key, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId && s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveSettingAsync(int userId, string key, string value, CancellationToken cancellationToken = default)
    {
        using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Key == key, cancellationToken);

        if (setting == null)
        {
            context.UserSettings.Add(new UserSetting
            {
                UserId = userId,
                Key = key,
                Value = value
            });
        }
        else
        {
            setting.Value = value;
            context.UserSettings.Update(setting);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
