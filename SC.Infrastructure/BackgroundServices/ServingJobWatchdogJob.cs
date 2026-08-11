using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Services;

namespace SC.Infrastructure.BackgroundServices;

public sealed class ServingJobWatchdogJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ServingJobWatchdogJob> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ServingJobWatchdogJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var watchdog = scope.ServiceProvider.GetRequiredService<IServingJobWatchdogService>();
                await watchdog.SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ServingJobWatchdogJob failed.");
            }
        }
    }
}
