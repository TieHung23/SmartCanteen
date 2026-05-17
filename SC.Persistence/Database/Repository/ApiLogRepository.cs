using Microsoft.EntityFrameworkCore;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Logging.AggregateRoot;
using SC.Domain.SharedKernel.Enums;

namespace SC.Persistence.Database.Repository;

public class ApiLogRepository : IApiLogRepository
{
    private readonly SmartCanteenDbContext _db;

    public ApiLogRepository(SmartCanteenDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ApiLog apiLog, AppLogLevel logLevel = AppLogLevel.DEBUG)
    {
        apiLog.Id = Guid.NewGuid();
        apiLog.LogLevel = ToStr(logLevel);

        apiLog.ErrorTrace = Escape(apiLog.ErrorTrace);
        apiLog.ApiBody = Escape(apiLog.ApiBody);
        apiLog.ApiResponse = Escape(apiLog.ApiResponse);

        _db.ApiLogs.Add(apiLog);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ApiLog>> GetOldLogsAsync(int daysOld = 60, int limit = 500)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-daysOld);
        return await _db.ApiLogs
            .AsNoTracking()
            .Where(x => x.CreatedDate < cutoff)
            .OrderBy(x => x.CreatedDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountOldLogsAsync(int daysOld = 60)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-daysOld);
        return await _db.ApiLogs
            .AsNoTracking()
            .CountAsync(x => x.CreatedDate < cutoff);
    }

    public async Task<int> DeleteByRequestIdsAsync(IEnumerable<string> requestIds)
    {
        var ids = requestIds.ToList();
        if (!ids.Any()) return 0;

        return await _db.ApiLogs
            .Where(x => ids.Contains(x.RequestId!))
            .ExecuteDeleteAsync();
    }

    private static string ToStr(AppLogLevel l) => l switch
    {
        AppLogLevel.INFO => "INFO",
        AppLogLevel.ERROR => "ERROR",
        _ => "DEBUG",
    };

    private static string Escape(string? s) =>
        string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\\", "\\\\");
}
