using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Oikos.Application.Data;

namespace Oikos.Application.Common;

public static class CustomerNumberHelper
{
    public static async Task<string> GenerateUniqueCustomerNumberAsync(
        IAppDbContext context,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"{DateTime.UtcNow:yyyyMMdd}{RandomNumberGenerator.GetInt32(0, 10000):D4}";

            var exists = await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.CustomerNumber == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }

            await Task.Delay(25, cancellationToken);
        }

        throw new InvalidOperationException("Unable to generate a unique customer number after multiple attempts.");
    }
}
