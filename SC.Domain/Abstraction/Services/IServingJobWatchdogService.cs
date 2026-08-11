namespace SC.Domain.Abstraction.Services;

/// <summary>
/// Watchdog cho serving job: dò job kẹt (Pushed/Assembling không có báo trong 1 khoảng)
/// = executor chết/mất kết nối → requeue để executor khác kéo lại; quá số lần → Failed.
/// (Lỗi "có báo" như verify-fail/hết hàng do executor tự report ở ReportServingStatus, KHÔNG qua đây.)
/// </summary>
public interface IServingJobWatchdogService
{
    Task SweepAsync(CancellationToken cancellationToken = default);
}
