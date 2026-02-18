using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Oikos.Infrastructure.Data;

public class OikosDbContextFactory : IDesignTimeDbContextFactory<OikosDbContext>
{
    public OikosDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetValue<string>("Application:ConnectionString")
            ?? throw new InvalidOperationException("Application:ConnectionString is not configured");
        var provider = configuration.GetValue<string>("Application:DatabaseProvider")
            ?? throw new InvalidOperationException("Application:DatabaseProvider is not configured");

        var optionsBuilder = new DbContextOptionsBuilder<OikosDbContext>();

        switch (provider)
        {
            case "SqlServer":
                optionsBuilder.UseSqlServer(connectionString);
                break;
            case "Sqlite":
                optionsBuilder.UseSqlite(connectionString);
                break;
            case "PostgreSQL":
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
                optionsBuilder.UseNpgsql(connectionString);
                break;
            case "MySQL":
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;
            default:
                throw new InvalidOperationException($"Unsupported provider '{provider}'.");
        }

        var services = new ServiceCollection();
        var providerServices = services.BuildServiceProvider();

        return new OikosDbContext(optionsBuilder.Options, providerServices);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var currentDirectory = Directory.GetCurrentDirectory();
        var builder = new ConfigurationBuilder();

        if (File.Exists(Path.Combine(currentDirectory, "appsettings.json")))
        {
            builder.SetBasePath(currentDirectory);
        }
        else
        {
            var webProjectPath = Path.GetFullPath(Path.Combine(currentDirectory, "..", "Oikos.Web"));
            builder.SetBasePath(webProjectPath);
        }

        builder
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
