using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Oikos.Infrastructure.Data;

public static class DatabaseExtension
{
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetValue<string>("Application:ConnectionString")!;
        var provider = builder.Configuration.GetValue<string>("Application:DatabaseProvider")!;

        builder.Services.AddDbContextFactory<OikosDbContext>(options =>
        {
            switch (provider)
            {
                case "SqlServer":
                    options.UseSqlServer(connectionString);
                    break;
                case "Sqlite":
                    options.UseSqlite(connectionString);
                    break;
                case "PostgreSQL":
                    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                    options.UseNpgsql(connectionString);
                    break;
                case "MySQL":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
            }
        }, ServiceLifetime.Scoped);

        return builder;
    }

    public static void InitialDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OikosDbContext>>();
        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
        InitialDataSeeder.Seed(context);
    }
}
