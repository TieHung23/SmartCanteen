using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Services;

namespace SC.Infrastructure.BackgroundServices;

public sealed class SessionFinalizationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionFinalizationJob> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SessionFinalizationJob is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var finalizeService = scope.ServiceProvider.GetRequiredService<IFinalizeSessionService>();
                await finalizeService.AutoFinalizeOverdueSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SessionFinalizationJob failed.");
            }
        }
    }
}
