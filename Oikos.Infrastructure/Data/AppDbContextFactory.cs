using Oikos.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace Oikos.Infrastructure.Data;

public class AppDbContextFactory : IAppDbContextFactory
{
    private readonly IDbContextFactory<OikosDbContext> _factory;

    public AppDbContextFactory(IDbContextFactory<OikosDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IAppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await _factory.CreateDbContextAsync(cancellationToken);
    }
}
