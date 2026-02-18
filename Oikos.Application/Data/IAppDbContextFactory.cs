namespace Oikos.Application.Data;

public interface IAppDbContextFactory
{
    Task<IAppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
