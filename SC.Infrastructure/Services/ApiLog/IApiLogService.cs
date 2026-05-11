using SC.Domain.SharedKernel.Enums;
using ApiLogEntity = SC.Domain.Domain.Logging.AggregateRoot.ApiLog;

namespace SC.Infrastructure.Services.ApiLog;

public interface IApiLogService
{
    Task WriteLogAsync(ApiLogEntity logItem, AppLogLevel logLevel = AppLogLevel.DEBUG);
}
