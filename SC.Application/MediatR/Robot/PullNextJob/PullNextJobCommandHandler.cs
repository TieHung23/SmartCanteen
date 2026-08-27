using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using SC.Domain.Domain.RobotEventLog.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;

namespace SC.Application.MediatR.Robot.PullNextJob;

internal sealed class PullNextJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<PullNextJobCommandHandler> logger
) : ICommandHandler<PullNextJobCommand, PullNextJobResponse>
{
    public async Task<Result<PullNextJobResponse>> Handle(
        PullNextJobCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            // 1) Job Queued cũ nhất (FIFO theo giờ tạo). Chưa có việc -> Job = null (204).
            var queuedJobs = await servingJobRepository
                .FindListAsync(x => x.Status == ServingJobStatus.Queued, cancellationToken);

            var orderedQueued = queuedJobs.OrderBy(x => x.CreatedAtUtc).ToList();
            if (orderedQueued.Count == 0)
            {
                return Result.Success(new PullNextJobResponse(null), "No queued job.");
            }

            // 1b) CHỈ phục vụ job của order thuộc CA đang MỞ (AvailableFrom <= now <= AvailableTo).
            //     Map order -> session rồi lọc theo giờ; job của CA chưa mở / đã đóng thì chờ.
            var now = DateTimeOffset.UtcNow;
            var queuedOrderIds = orderedQueued.Select(x => x.OrderId).Distinct().ToList();
            var queuedOrders = await orderRepository.FindListAsync(
                x => queuedOrderIds.Contains(x.Id), cancellationToken);

            // 1c) Đơn đã HỦY/HẾT HẠN/HOÀN TẤT mà job còn Queued (luồng hủy/refund/pickup không đụng ServingJob)
            //     -> HỦY job + trả khay đang giữ (requeue) về pool. Tuyệt đối không phục vụ đơn đã kết thúc.
            //     Bất biến: Order ở trạng thái kết thúc (Cancelled/Expired/Completed) ⟹ Job cũng phải kết thúc
            //     (chặn double-serve: job Queued mồ côi của đơn đã Completed bị pull + phục vụ lại).
            var deadOrderIds = queuedOrders
                .Where(x => x.Status is OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Completed)
                .Select(x => x.Id)
                .ToHashSet();
            if (deadOrderIds.Count > 0)
            {
                foreach (var deadJob in orderedQueued.Where(j => deadOrderIds.Contains(j.OrderId)))
                {
                    if (deadJob.TrayId is Guid heldId)
                    {
                        var heldTray = await trayRepository.GetByIdAsync(heldId, cancellationToken);
                        if (heldTray is not null)
                        {
                            heldTray.Release(actorId);
                            trayRepository.Update(heldTray);
                        }
                        deadJob.ClearTray(actorId);
                    }
                    deadJob.Cancel(actorId);
                    servingJobRepository.Update(deadJob);
                    logger.LogInformation(
                        "Cancelled queued serving job {JobId}: order {OrderId} is cancelled/expired/completed.",
                        deadJob.Id, deadJob.OrderId);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
                orderedQueued = orderedQueued.Where(j => !deadOrderIds.Contains(j.OrderId)).ToList();
            }

            var sessionIdByOrder = queuedOrders.ToDictionary(x => x.Id, x => x.SessionId);

            var sessionIds = sessionIdByOrder.Values.Distinct().ToList();
            var activeSessions = await sessionRepository.FindListAsync(
                x => sessionIds.Contains(x.Id)
                     && !x.IsDeleted
                     && x.AvailableFrom <= now
                     && now <= x.AvailableTo,
                cancellationToken);
            var activeSessionIds = activeSessions.Select(x => x.Id).ToHashSet();

            // candidate = job Queued của ca ĐANG MỞ, GIỮ thứ tự FIFO (orderedQueued đã sort theo CreatedAtUtc).
            var candidates = orderedQueued
                .Where(j => sessionIdByOrder.TryGetValue(j.OrderId, out var sid) && activeSessionIds.Contains(sid))
                .ToList();
            if (candidates.Count == 0)
            {
                return Result.Success(new PullNextJobResponse(null), "No queued job for an active session.");
            }

            // #4 TRAY-FIRST: chọn job theo KHAY Unity quét được — GIỮ NGUYÊN FIFO:
            //   - trayCode rỗng (non-camera / cũ): FIFO-oldest như cũ.
            //   - khay Available: FIFO-oldest job MỚI (chưa có khay) -> sẽ bind khay này. Job requeue (đã có
            //     khay) KHÔNG lấy khay khác -> bỏ qua, chờ đúng khay Reserved của nó.
            //   - khay Reserved của 1 job Queued: CHÍNH job đó (resume trên khay chứa món đã gắp). Khay dành
            //     riêng job đó nên không "chen hàng" FIFO của job khác (job khác không dùng được khay này).
            //   - khay Reserved mồ côi / chưa đăng ký / InUse: null (không phục vụ bừa -> tránh bind hụt -> fail oan).
            ServingJobEntity? job;
            var scannedCode = (request.TrayCode ?? string.Empty).Trim();
            if (scannedCode.Length == 0)
            {
                job = candidates[0];
            }
            else
            {
                var scannedTray = await trayRepository.FindSingleAsync(
                    x => x.Code == scannedCode && !x.IsDeleted, cancellationToken);
                if (scannedTray is null)
                {
                    return Result.Success(new PullNextJobResponse(null), $"Tray '{scannedCode}' chưa đăng ký.");
                }

                if (scannedTray.Status == TrayStatus.Reserved)
                {
                    job = candidates.FirstOrDefault(j => j.TrayId == scannedTray.Id);
                    if (job is null)
                    {
                        return Result.Success(new PullNextJobResponse(null),
                            $"Tray '{scannedCode}' đang Reserved nhưng không có job chờ nào giữ nó.");
                    }
                }
                else if (scannedTray.Status == TrayStatus.Available)
                {
                    job = candidates.FirstOrDefault(j => j.TrayId is null);
                    if (job is null)
                    {
                        return Result.Success(new PullNextJobResponse(null),
                            "Không có job mới (chưa có khay) để gán khay Available này.");
                    }
                }
                else
                {
                    return Result.Success(new PullNextJobResponse(null),
                        $"Tray '{scannedCode}' không dùng được (status: {scannedTray.Status}).");
                }
            }

            // 2) Khay: KHÔNG auto-gán ở đây nữa — edge quét mã khay VẬT LÝ rồi gọi bind-tray
            //    (đảm bảo TrayId khớp khay thật, để pickup không lệch). Job MỚI -> tray=null.
            //    Job REQUEUE đã có khay từ lượt trước (món đã gắp còn trên đó) -> pass-through
            //    để edge biết khay nào, KHỎI quét lại.
            TrayEntity? tray = job.TrayId is Guid heldTrayId
                ? await trayRepository.GetByIdAsync(heldTrayId, cancellationToken)
                : null;

            // 3) Lấy order + items
            var order = await orderRepository.GetByIdAsync(
                job.OrderId, cancellationToken, o => o.OrderItems);
            if (order is null)
            {
                job.Cancel(actorId);                      // order biến mất -> huỷ job, bỏ qua
                servingJobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(new PullNextJobResponse(null), "Order missing; job cancelled.");
            }


            // 4) Nhãn cho robot: món -> LaneCode (gắp Ở ĐÂU) + Station (TAY nào), theo cấu hình CA này.
            //    Đọc TRƯỚC khi claim job: query lỗi thì job không bị kẹt ở Pushed mà chẳng ai làm.
            //    Món chưa cấu hình lane -> LaneCode/Station = null (không fail pull; edge/staff xử lý).
            var dishIds = order.OrderItems.Select(i => i.DishId).Distinct().ToList();

            var configs = await slotConfigurationRepository.FindListAsync(
                x => x.SessionId == order.SessionId && !x.IsDeleted, cancellationToken);
            var configByDish = configs
                .GroupBy(x => x.DishId)
                .ToDictionary(g => g.Key, g => g.First());

            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNameById = dishes.ToDictionary(x => x.Id, x => x.Name);

            var armIds = configByDish.Values
                .Where(x => x.RobotArmId.HasValue)
                .Select(x => x.RobotArmId!.Value)
                .Distinct()
                .ToList();
            var arms = armIds.Count == 0
                ? new List<RobotArmEntity>()
                : await robotArmRepository.FindListAsync(x => armIds.Contains(x.Id), cancellationToken);
            var armCodeById = arms.ToDictionary(x => x.Id, x => x.Code);

            // 5) Claim job (Queued->Pushed). CHƯA gán khay — edge quét khay thật rồi gọi bind-tray.
            //     (Requeue: job giữ nguyên TrayId cũ, không đụng.)
            job.MarkPushed(actorId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 5b) RESUME (#10 count-based): SỐ TÔ đã PlaceCompleted lượt trước, ĐẾM PER DishId
            //     (RobotEventLogs — nguồn chân lý, sống qua restart edge). Requeue -> edge chỉ đặt
            //     (Quantity - PlacedCount) tô còn thiếu; đủ số (Done) thì SKIP hẳn.
            var placedLogs = await robotEventLogRepository.FindListAsync(
                x => x.ServingJobId == job.Id
                     && x.EventType == RobotEventType.PlaceCompleted
                     && x.DishId != null,
                cancellationToken);
            var placedCountByDish = placedLogs
                .GroupBy(x => x.DishId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var message = new ServingJobMessage
            {
                JobId = job.Id,
                OrderId = order.Id,
                TrayId = job.TrayId,      // null cho job mới (edge quét+bind); có sẵn cho requeue
                TrayCode = tray?.Code,    // null cho job mới
                // GỘP theo DishId + Σ Quantity: 1 món = 1 item (dù đơn có nhiều OrderItem cùng dish).
                //   qty=1 -> mỗi món 1 item như cũ; qty>1 -> Quantity mang số tô.
                Items = order.OrderItems
                    .GroupBy(i => i.DishId)
                    .Select(g =>
                    {
                        var dishId = g.Key;
                        var quantity = g.Sum(i => i.Quantity);
                        configByDish.TryGetValue(dishId, out var cfg);
                        string? station = null;
                        if (cfg?.RobotArmId is Guid armId && armCodeById.TryGetValue(armId, out var code))
                            station = code;
                        var placed = placedCountByDish.TryGetValue(dishId, out var pc) ? pc : 0;

                        return new ServingJobItemMessage
                        {
                            DishId = dishId,
                            DishName = dishNameById.TryGetValue(dishId, out var name) ? name : null,
                            Quantity = quantity,
                            Station = station,
                            LaneCode = cfg?.LaneCode,
                            PlacedCount = placed,
                            Done = placed >= quantity   // đặt đủ số tô -> edge SKIP
                        };
                    }).ToList()
            };

            return Result.Success(new PullNextJobResponse(message), "Job dispatched.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pulling next serving job");
            return Result.Failure<PullNextJobResponse>(
                Error.ServerError, "An error occurred while pulling the next job.");
        }
    }
}
