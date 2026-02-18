using Oikos.Infrastructure.Data;
using Oikos.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oikos.Common.Helpers;
using Oikos.Application.Services.Invoice.Models;

namespace Oikos.Infrastructure.BackgroundServices;

public class InvoiceOcrBackgroundService : BackgroundService
{
    private readonly ILogger<InvoiceOcrBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InvoiceOcrBackgroundService(ILogger<InvoiceOcrBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await ChannelHelper<InvoiceProcessRequest>.Instance.Reader.ReadAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OikosDbContext>>();
                using var context = await dbFactory.CreateDbContextAsync(stoppingToken);

                var invoice = await context.Invoices.FirstOrDefaultAsync(x => x.Id == request.InvoiceId, stoppingToken);
                if (invoice == null) continue;

                // Mock OCR extraction
                await Task.Delay(500, stoppingToken);
                invoice.Company = invoice.Company ?? "ACME Inc.";
                invoice.Amount ??= "123.45";
                invoice.InvoiceDate = invoice.InvoiceDate ?? DateTime.Today;
                invoice.Currency = invoice.Currency ?? "USD";
                invoice.Description = invoice.Description ?? "OCR parsed description";
                invoice.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
