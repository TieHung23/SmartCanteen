namespace SC.Persistence.Database.Services;

/// <summary>
/// Chống-spam cho cảnh báo "job Queued mồ côi" của watchdog (SINGLETON, in-memory).
/// Watchdog quét 30s/lần nhưng cảnh báo chỉ được LẶP mỗi <c>interval</c> (mặc định ~5').
/// Khi hết job chờ / executor quay lại -> <see cref="Reset"/> để lần kẹt SAU được báo ngay,
/// không phải chờ hết interval.
/// </summary>
public sealed class ServingStuckAlertThrottle
{
    private readonly object _lock = new();
    private DateTimeOffset? _lastAlertUtc;

    /// <summary>true nếu đủ điều kiện báo LẦN NÀY (lần đầu, hoặc đã qua <paramref name="interval"/>
    /// kể từ lần báo trước) — đồng thời ghi nhận mốc báo.</summary>
    public bool ShouldAlert(DateTimeOffset now, TimeSpan interval)
    {
        lock (_lock)
        {
            if (_lastAlertUtc is null || now - _lastAlertUtc.Value >= interval)
            {
                _lastAlertUtc = now;
                return true;
            }
            return false;
        }
    }

    /// <summary>Không còn gì để cảnh báo (hết job chờ / executor về) -> mở cho lần kẹt sau báo ngay.</summary>
    public void Reset()
    {
        lock (_lock) { _lastAlertUtc = null; }
    }
}
