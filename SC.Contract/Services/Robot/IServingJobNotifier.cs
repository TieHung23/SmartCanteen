namespace SC.Contract.Services.Robot;

/// <summary>
/// Đẩy job phục vụ cho robot service (PUSH/BUFFER). Hiện thực bằng SignalR ở tầng Api.
/// </summary>
public interface IServingJobNotifier
{
    Task PushJobAsync(ServingJobMessage job, CancellationToken cancellationToken = default);

    /// <summary>Hybrid: chỉ ĐÁNH THỨC robot (event "JobAvailable", KHÔNG kèm data) → robot tự pull next-job.</summary>
    Task PingNewJobAsync(CancellationToken cancellationToken = default);
}
