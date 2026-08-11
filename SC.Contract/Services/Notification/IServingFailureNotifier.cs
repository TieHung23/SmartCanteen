namespace SC.Contract.Services.Notification;

/// <summary>
/// Báo cho toàn bộ nhân viên (role Staff) khi 1 serving job bị lỗi cần can thiệp.
/// Dùng chung: ReportServingStatus (executor tự báo Failed) + Watchdog (job treo give-up).
/// </summary>
public interface IServingFailureNotifier
{
    Task NotifyStaffAsync(Guid orderId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Báo cho TOÀN BỘ Staff bằng 1 template bất kỳ (đã lấy / đã ráp xong / ...).</summary>
    Task NotifyAllStaffAsync(
        string templateKey,
        Guid orderId,
        IReadOnlyDictionary<string, string> tokens,
        object? data = null,
        CancellationToken cancellationToken = default);
}
