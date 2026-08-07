using Microsoft.Extensions.Logging;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Persistence.Database.Services;

public sealed class ServingJobWatchdogService(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IUnitOfWork unitOfWork,
    ILogger<ServingJobWatchdogService> logger) : IServingJobWatchdogService
{
    // Job Pushed/Assembling mà không có báo (UpdatedAtUtc cũ) quá ngưỡng này = TREO
    //   (executor chết / mất kết nối / tắt Play giữa chừng). Mỗi ReportStatus đều Touch job,
    //   nên job đang phục vụ thật có UpdatedAtUtc mới -> không bị coi là treo.
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromSeconds(60);

    // Requeue tối đa N lần; quá thì cho Failed (chống job hỏng vĩnh viễn kẹt đầu hàng).
    private const int MaxRequeue = 3;

    private static readonly Guid SystemActor = Guid.Empty;

    public async Task SweepAsync(CancellationToken cancellationToken = default)
    {
        var staleBefore = DateTimeOffset.UtcNow - StuckThreshold;

        var stuckJobs = await servingJobRepository.FindListAsync(
            job => !job.IsDeleted
                   && (job.Status == ServingJobStatus.Pushed
                       || job.Status == ServingJobStatus.Assembling)
                   && job.UpdatedAtUtc != null
                   && job.UpdatedAtUtc < staleBefore,
            cancellationToken);

        if (stuckJobs.Count == 0)
        {
            return;
        }

        var requeued = 0;
        var failed = 0;

        foreach (var job in stuckJobs)
        {
            if (job.RequeueCount >= MaxRequeue)
            {
                // Bó tay: đánh Failed. (Notify staff sẽ gắn ở bước sau - task #9.)
                job.MarkFailed(
                    $"Watchdog: job treo quá {MaxRequeue} lần thử (executor mất kết nối / không báo).",
                    SystemActor);
                failed++;
            }
            else
            {
                // Requeue: về Queued, GIỮ TrayId (món đã gắp còn trên khay) -> executor kéo lại làm tiếp.
                job.Requeue(SystemActor);
                requeued++;
            }

            servingJobRepository.Update(job);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ServingJobWatchdog: {Requeued} requeued, {Failed} failed / {Total} job treo.",
            requeued, failed, stuckJobs.Count);
    }
}
