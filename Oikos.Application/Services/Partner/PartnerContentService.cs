using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Constants;
using Oikos.Application.Data;
using Oikos.Application.Services.Partner.Models;
using Oikos.Domain.Entities.Setting;

namespace Oikos.Application.Services.Partner;

public class PartnerContentService : IPartnerContentService
{
    private readonly IAppDbContextFactory _dbContextFactory;
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public PartnerContentService(IAppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PartnerContent> GetContentAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.Settings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == PartnerConstants.SettingKey, cancellationToken);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return new PartnerContent();
        }

        try
        {
            var content = JsonSerializer.Deserialize<PartnerContent>(setting.Value, _serializerOptions);
            return content ?? new PartnerContent();
        }
        catch
        {
            return new PartnerContent();
        }
    }

    public async Task SaveContentAsync(PartnerContent content, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.Key == PartnerConstants.SettingKey, cancellationToken);
        var json = JsonSerializer.Serialize(content, _serializerOptions);

        if (setting == null)
        {
            context.Settings.Add(new Domain.Entities.Setting.Setting
            {
                Key = PartnerConstants.SettingKey,
                Value = json
            });
        }
        else
        {
            setting.Value = json;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
