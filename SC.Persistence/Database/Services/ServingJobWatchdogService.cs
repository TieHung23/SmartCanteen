using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm.Enum;
using SC.Domain.Domain.RobotEventLog.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Persistence.Database.Services;

public sealed class ServingJobWatchdogService(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
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
        var skippedAssembled = 0;

        foreach (var job in stuckJobs)
        {
            // Assembling mà ĐÃ RÁP XONG HẾT MÓN (tất cả item đã PlaceCompleted) = đang CHỜ STAFF quét lên kệ,
            //   KHÔNG phải treo -> BỎ QUA (fail oan đơn đã ráp xong sẽ phá luồng; việc staff không lên kệ
            //   do cơ chế hết-hạn-pickup lo, không phải watchdog).
            if (job.Status == ServingJobStatus.Assembling
                && await IsAssemblyCompleteAsync(job, cancellationToken))
            {
                skippedAssembled++;
                continue;
            }

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
                "ServingJobWatchdog: {Requeued} requeued, {Failed} failed, {Skipped} đã-ráp-xong bỏ qua / {Total} nghi treo; {Arms} arm -> Offline.",
                requeued, failedOrderIds.Count, skippedAssembled, stuckJobs.Count, staleArms.Count);
        }
    }

    // Job Assembling ĐÃ RÁP XONG HẾT MÓN (tất cả DishId của đơn đều có log PlaceCompleted) = đang CHỜ STAFF
    //   quét lên kệ, KHÔNG phải treo. Watchdog BỎ QUA để tránh fail oan đơn đã ráp xong mà staff chưa kịp quét.
    private async Task<bool> IsAssemblyCompleteAsync(ServingJobEntity job, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(job.OrderId, ct, o => o.OrderItems);
        if (order is null) return false;   // order mất -> để requeue/fail xử lý bình thường

        var neededDishIds = order.OrderItems.Select(i => i.DishId).Distinct().ToHashSet();
        if (neededDishIds.Count == 0) return false;

        var placedLogs = await robotEventLogRepository.FindListAsync(
            x => x.ServingJobId == job.Id
                 && x.EventType == RobotEventType.PlaceCompleted
                 && x.DishId != null,
            ct);
        var placedDishIds = placedLogs.Select(x => x.DishId!.Value).ToHashSet();

        return neededDishIds.IsSubsetOf(placedDishIds);   // tất cả món cần đều đã đặt -> ráp xong
    }
}
