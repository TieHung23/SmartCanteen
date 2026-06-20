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

    public async Task<List<ApiLog>> GetFilteredLogsAsync(
        string? logLevel,
        string? method,
        string? url,
        int? statusCodeMin,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken ct = default)
    {
        var query = _db.ApiLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(logLevel))
            query = query.Where(x => x.LogLevel == logLevel);

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(x => x.ApiMethod == method);

        if (!string.IsNullOrWhiteSpace(url))
            query = query.Where(x => x.ApiUrl.Contains(url));

        if (statusCodeMin.HasValue)
            query = query.Where(x => x.StatusCode >= statusCodeMin.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CreatedDate <= toDate.Value);

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(ct);
    }

    public async Task<ApiLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ApiLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
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
