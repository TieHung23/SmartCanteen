using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm.Enum;
using SC.Domain.Domain.RobotEventLog.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Persistence.Database.Services;

public sealed class ServingJobWatchdogService(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IServingFailureNotifier servingFailureNotifier,
    ServingStuckAlertThrottle alertThrottle,
    IConfiguration config,
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
                    $"Đơn bị treo — mất kết nối với robot, đã thử lại {MaxRequeue} lần không xong.",
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
                "Đơn bị treo — mất kết nối với robot, đã thử lại nhiều lần không xong.",
                cancellationToken);
        }

        if (changed)
        {
            logger.LogInformation(
                "ServingJobWatchdog: {Requeued} requeued, {Failed} failed, {Skipped} đã-ráp-xong bỏ qua / {Total} nghi treo; {Arms} arm -> Offline.",
                requeued, failedOrderIds.Count, skippedAssembled, stuckJobs.Count, staleArms.Count);
        }

        // ===== 4) CẢNH BÁO (KHÔNG đổi state) job Queued MỒ CÔI trong ca đang mở =====
        //   #3: máy CHỈ BÁO, KHÔNG tự khai tử job Queued (chờ lâu có thể do executor vắng HOẶC
        //   hàng đông = bình thường). Staff nhận cảnh báo -> tự quyết qua endpoint /fail.
        await AlertStuckQueuedJobsAsync(now, cancellationToken);
    }

    // #3: quét job Queued MỒ CÔI (đơn thuộc ca ĐANG MỞ mà chưa executor nào nhận) -> CHỈ CẢNH BÁO GỘP
    //   cho staff, KHÔNG đổi state. Báo khi (a) executor VẮNG (mọi arm Offline) hoặc (b) đơn lâu nhất
    //   vượt ngưỡng cấu hình; chống spam bằng ServingStuckAlertThrottle (lặp ~5', reset khi hết kẹt).
    private async Task AlertStuckQueuedJobsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var openSessions = await sessionRepository.FindListAsync(
            s => !s.IsDeleted && s.AvailableFrom <= now && now <= s.AvailableTo, ct);
        var openSessionIds = openSessions.Select(s => s.Id).ToHashSet();
        if (openSessionIds.Count == 0) { alertThrottle.Reset(); return; }

        var queuedJobs = await servingJobRepository.FindListAsync(
            j => !j.IsDeleted && j.Status == ServingJobStatus.Queued, ct);
        if (queuedJobs.Count == 0) { alertThrottle.Reset(); return; }

        // chỉ tính job của order thuộc CA ĐANG MỞ (job của ca chưa mở/đã đóng không phải "mồ côi cần cứu").
        var queuedOrderIds = queuedJobs.Select(j => j.OrderId).Distinct().ToList();
        var openOrders = await orderRepository.FindListAsync(
            o => queuedOrderIds.Contains(o.Id) && openSessionIds.Contains(o.SessionId), ct);
        var openOrderIds = openOrders.Select(o => o.Id).ToHashSet();
        var stuckJobs = queuedJobs.Where(j => openOrderIds.Contains(j.OrderId)).ToList();
        if (stuckJobs.Count == 0) { alertThrottle.Reset(); return; }

        // executor VẮNG = mọi arm đã đăng ký đều Offline (heartbeat quá hạn — mục 2 vừa cập nhật trong sweep này).
        var arms = await robotArmRepository.FindListAsync(a => !a.IsDeleted, ct);
        var executorAbsent = arms.Count > 0 && arms.All(a => a.Status == RobotArmStatus.Offline);

        var oldestJob = stuckJobs.OrderBy(j => j.CreatedAtUtc).First();
        var oldestAgeMin = (int)Math.Floor((now - oldestJob.CreatedAtUtc).TotalMinutes);
        var thresholdMin = int.TryParse(config["Serving:StuckAlertMinutes"], out var t) ? t : 15;
        var overThreshold = oldestAgeMin >= thresholdMin;

        if (!executorAbsent && !overThreshold) return;   // chờ ngắn + executor còn đó = bình thường, chưa báo

        var repeatMin = Math.Max(1, int.TryParse(config["Serving:StuckAlertRepeatMinutes"], out var r) ? r : 5);
        if (!alertThrottle.ShouldAlert(now, TimeSpan.FromMinutes(repeatMin))) return;

        var reason = executorAbsent
            ? $"Robot đang mất kết nối — {stuckJobs.Count} đơn đang chờ phục vụ (đơn lâu nhất {oldestAgeMin} phút). " +
              "Kiểm tra robot; nếu cần, đánh dấu đơn lỗi (fail) để xử lý thủ công."
            : $"{stuckJobs.Count} đơn đang chờ robot phục vụ, đơn lâu nhất đã {oldestAgeMin} phút. Kiểm tra tình trạng phục vụ.";

        // Báo TOÀN BỘ staff bằng template "đang chờ" (KHÔNG mượn template "lỗi" gây hiểu nhầm); tham chiếu
        // đơn LÂU NHẤT để staff bấm vào xử lý trước (đánh /fail rồi Requeue/ManualComplete).
        await servingFailureNotifier.NotifyAllStaffAsync(
            NotificationTemplateKeys.ServingStuckAlert,
            oldestJob.OrderId,
            new Dictionary<string, string>
            {
                ["referenceId"] = oldestJob.OrderId.ToString(),
                ["orderId"] = oldestJob.OrderId.ToString(),
                ["reason"] = reason
            },
            new { OrderId = oldestJob.OrderId, Reason = reason },
            ct);
        logger.LogWarning(
            "ServingJobWatchdog ALERT: {Count} job Queued mồ côi (executorAbsent={Absent}, oldest={Age} phút).",
            stuckJobs.Count, executorAbsent, oldestAgeMin);
    }

    // Job Assembling ĐÃ RÁP XONG HẾT MÓN (tất cả DishId của đơn đều có log PlaceCompleted) = đang CHỜ STAFF
    //   quét lên kệ, KHÔNG phải treo. Watchdog BỎ QUA để tránh fail oan đơn đã ráp xong mà staff chưa kịp quét.
    private async Task<bool> IsAssemblyCompleteAsync(ServingJobEntity job, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(job.OrderId, ct, o => o.OrderItems);
        if (order is null) return false;   // order mất -> để requeue/fail xử lý bình thường

        // #10: cần ĐỦ SỐ LƯỢNG mỗi món (Σ Quantity per DishId), không chỉ "có mặt".
        var neededByDish = order.OrderItems
            .GroupBy(i => i.DishId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (neededByDish.Count == 0) return false;

        var placedLogs = await robotEventLogRepository.FindListAsync(
            x => x.ServingJobId == job.Id
                 && x.EventType == RobotEventType.PlaceCompleted
                 && x.DishId != null,
            ct);
        var placedByDish = placedLogs
            .GroupBy(x => x.DishId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // đủ khi MỌI món số tô đã đặt >= số cần (qty=1 -> giống "có mặt" cũ) -> ráp xong.
        return neededByDish.All(kv =>
            placedByDish.TryGetValue(kv.Key, out var placed) && placed >= kv.Value);
    }
}
