using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Persistence.Database.Services;

public sealed class ServingJobWatchdogService(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IServingFailureNotifier servingFailureNotifier,
    IUnitOfWork unitOfWork,
    ILogger<ServingJobWatchdogService> logger) : IServingJobWatchdogService
{
    // Job Pushed/Assembling mà không có báo (UpdatedAtUtc cũ) quá ngưỡng này = TREO
    //   (executor chết / mất kết nối). Mỗi ReportStatus đều Touch job -> job phục vụ thật ko bị coi treo.
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromSeconds(60);

    // Arm không gửi heartbeat quá ngưỡng này -> coi như Offline.
    private static readonly TimeSpan ArmOfflineThreshold = TimeSpan.FromSeconds(90);

    // Requeue tối đa N lần; quá thì Failed (chống job hỏng vĩnh viễn kẹt đầu hàng).
    private const int MaxRequeue = 3;

    private static readonly Guid SystemActor = Guid.Empty;

    public async Task SweepAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = false;

        // ===== 1) JOB TREO -> requeue / (quá số lần) Failed =====
        var staleBefore = now - StuckThreshold;
        var stuckJobs = await servingJobRepository.FindListAsync(
            job => !job.IsDeleted
                   && (job.Status == ServingJobStatus.Pushed
                       || job.Status == ServingJobStatus.Assembling)
                   && job.UpdatedAtUtc != null
                   && job.UpdatedAtUtc < staleBefore,
            cancellationToken);

        var failedOrderIds = new List<Guid>();   // job đánh Failed -> báo staff SAU commit
        var requeued = 0;

        foreach (var job in stuckJobs)
        {
            if (job.RequeueCount >= MaxRequeue)
            {
                job.MarkFailed(
                    $"Watchdog: job treo quá {MaxRequeue} lần thử (executor mất kết nối / không báo).",
                    SystemActor);
                failedOrderIds.Add(job.OrderId);
            }
            else
            {
                job.Requeue(SystemActor);   // -> Queued, giữ TrayId, RequeueCount++
                requeued++;
            }

            servingJobRepository.Update(job);
            changed = true;
        }

        // ===== 2) ARM MẤT NHỊP TIM -> Offline (bỏ qua Maintenance: cố tình tắt) =====
        var armStaleBefore = now - ArmOfflineThreshold;
        var staleArms = await robotArmRepository.FindListAsync(
            arm => !arm.IsDeleted
                   && arm.Status != RobotArmStatus.Offline
                   && arm.Status != RobotArmStatus.Maintenance
                   && arm.LastHeartbeatUtc != null
                   && arm.LastHeartbeatUtc < armStaleBefore,
            cancellationToken);

        foreach (var arm in staleArms)
        {
            arm.UpdateStatus(RobotArmStatus.Offline, SystemActor);
            robotArmRepository.Update(arm);
            changed = true;
        }

        if (changed)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // ===== 3) Báo staff cho các job Failed (SAU commit, giống pattern FinalizeSessionService) =====
        foreach (var orderId in failedOrderIds)
        {
            await servingFailureNotifier.NotifyStaffAsync(
                orderId,
                "Job treo — executor mất kết nối, đã quá số lần thử lại.",
                cancellationToken);
        }

        if (changed)
        {
            logger.LogInformation(
                "ServingJobWatchdog: {Requeued} requeued, {Failed} failed / {Total} treo; {Arms} arm -> Offline.",
                requeued, failedOrderIds.Count, stuckJobs.Count, staleArms.Count);
        }
    }
}
