using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.SharedKernel.Enums;
using ApiLogEntity = SC.Domain.Domain.Logging.AggregateRoot.ApiLog;

namespace SC.Infrastructure.Services.ApiLog;

public class ApiLogService : IApiLogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiLogService> _logger;

    public ApiLogService(IServiceProvider serviceProvider, ILogger<ApiLogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task WriteLogAsync(ApiLogEntity logItem, AppLogLevel logLevel = AppLogLevel.DEBUG)
    {
        try
        {
            // Resolve repository from a dedicated new scope to avoid interfering with any ongoing transaction
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApiLogRepository>();

            await repo.SaveAsync(logItem, logLevel);
        }
        catch (Exception ex)
        {
            // Fallback to text file log if DB write fails
            _logger.LogError(ex, "Failed to write DB ApiLog! Fallback to standard error log.");
        }
    }
}
