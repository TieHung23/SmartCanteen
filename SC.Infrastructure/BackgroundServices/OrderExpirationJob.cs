using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Services;

namespace SC.Infrastructure.BackgroundServices;

public sealed class OrderExpirationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderExpirationJob> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderExpirationJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expirationService = scope.ServiceProvider.GetRequiredService<IOrderExpirationService>();
                await expirationService.ExpireOverdueOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrderExpirationJob failed.");
            }
        }
    }
}
