using SC.Domain.Domain.Logging.AggregateRoot;
using SC.Domain.SharedKernel.Enums;

namespace SC.Domain.Abstraction.Repositories;

public interface IApiLogRepository
{
    Task SaveAsync(ApiLog apiLog, AppLogLevel logLevel = AppLogLevel.DEBUG);
    Task<List<ApiLog>> GetOldLogsAsync(int daysOld = 60, int limit = 500);
    Task<int> CountOldLogsAsync(int daysOld = 60);
    Task<int> DeleteByRequestIdsAsync(IEnumerable<string> requestIds);
}
