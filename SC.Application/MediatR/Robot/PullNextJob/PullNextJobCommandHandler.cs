using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
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
            var sessionIdByOrder = queuedOrders.ToDictionary(x => x.Id, x => x.SessionId);

            var sessionIds = sessionIdByOrder.Values.Distinct().ToList();
            var activeSessions = await sessionRepository.FindListAsync(
                x => sessionIds.Contains(x.Id)
                     && !x.IsDeleted
                     && x.AvailableFrom <= now
                     && now <= x.AvailableTo,
                cancellationToken);
            var activeSessionIds = activeSessions.Select(x => x.Id).ToHashSet();

            var job = orderedQueued.FirstOrDefault(j =>
                sessionIdByOrder.TryGetValue(j.OrderId, out var sid)
                && activeSessionIds.Contains(sid));
            if (job is null)
            {
                return Result.Success(new PullNextJobResponse(null), "No queued job for an active session.");
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

            // 5b) RESUME: món đã PlaceCompleted ở lượt trước (đọc RobotEventLogs — nguồn chân lý,
            //     sống qua restart edge). Requeue -> gắn Done=true để edge SKIP, khỏi gắp lại.
            var placedLogs = await robotEventLogRepository.FindListAsync(
                x => x.ServingJobId == job.Id
                     && x.EventType == RobotEventType.PlaceCompleted
                     && x.DishId != null,
                cancellationToken);
            var servedDishIds = placedLogs.Select(x => x.DishId!.Value).ToHashSet();

            var message = new ServingJobMessage
            {
                JobId = job.Id,
                OrderId = order.Id,
                TrayId = job.TrayId,      // null cho job mới (edge quét+bind); có sẵn cho requeue
                TrayCode = tray?.Code,    // null cho job mới
                Items = order.OrderItems.Select(i =>
                {
                    configByDish.TryGetValue(i.DishId, out var cfg);
                    string? station = null;
                    if (cfg?.RobotArmId is Guid armId && armCodeById.TryGetValue(armId, out var code))
                        station = code;

                    return new ServingJobItemMessage
                    {
                        DishId = i.DishId,
                        DishName = dishNameById.TryGetValue(i.DishId, out var name) ? name : null,
                        Quantity = i.Quantity,
                        Station = station,
                        LaneCode = cfg?.LaneCode,
                        Done = servedDishIds.Contains(i.DishId)
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
