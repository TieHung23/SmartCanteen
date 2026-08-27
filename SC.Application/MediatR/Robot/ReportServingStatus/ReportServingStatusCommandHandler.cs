using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Notification;
using SC.Contract.Services.Visualization;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.RobotEventLog.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Robot.ReportServingStatus;

internal sealed class ReportServingStatusCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingVisualizer servingVisualizer,
    IServingFailureNotifier servingFailureNotifier,
    IBusinessNotificationService businessNotificationService,
    IOrderStatusNotifier orderStatusNotifier,
    ILogger<ReportServingStatusCommandHandler> logger
) : ICommandHandler<ReportServingStatusCommand, ReportServingStatusResponse>
{
    public async Task<Result<ReportServingStatusResponse>> Handle(
        ReportServingStatusCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var jobs = await servingJobRepository
                .FindListAsync(x => x.OrderId == request.OrderId
                                   && x.Status != ServingJobStatus.Cancelled
                                   && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
            var job = jobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();

            if (job is null)
            {
                logger.LogWarning("ReportStatus for order {OrderId} but no active serving job found", request.OrderId);
                return Result.Failure<ReportServingStatusResponse>(
                    Error.ServingJobNotFound, "No active serving job for this order.");
            }

            var state = (request.State ?? string.Empty).Trim();
            var eventType = MapEventType(state);

            switch (eventType)
            {
                case RobotEventType.PickStarted:
                case RobotEventType.JobReceived:
                    if (job.Status == ServingJobStatus.Pushed)
                        job.Acknowledge(actorId);
                    break;

                case RobotEventType.Error:
                    job.MarkFailed(request.Message ?? state, actorId);
                    break;
            }

            servingJobRepository.Update(job);

            // "Tay nào làm" resolve từ station robot báo (S1/S2/S3) — job KHÔNG giữ RobotArmId
            // (mô hình dây chuyền: 1 đơn nhiều tay; attribution ở log mức món).
            Guid? robotArmId = null;
            if (!string.IsNullOrWhiteSpace(request.Station))
            {
                var arm = await robotArmRepository.FindSingleAsync(
                    x => x.Code == request.Station.Trim(), cancellationToken);
                robotArmId = arm?.Id;

                // Tay vừa báo việc = tay còn sống -> nhịp tim "miễn phí" (LastHeartbeatUtc, Offline->Idle)
                if (arm is not null)
                {
                    arm.Heartbeat(actorId);
                    robotArmRepository.Update(arm);
                }
            }

            await robotEventLogRepository.AddAsync(
                RobotEventLogEntity.Create(
                    eventType,
                    actorId,
                    robotArmId: robotArmId,
                    servingJobId: job.Id,
                    orderId: job.OrderId,
                    dishId: request.DishId,
                    message: request.Message ?? state),
                cancellationToken);

            // Đồng bộ Order status khi robot bắt đầu ráp
            Guid? notifyPreparingStudentId = null;
            if (eventType is RobotEventType.PickStarted or RobotEventType.JobReceived)
            {
                var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order is not null && order.Status == OrderStatus.Pending)
                {
                    order.UpdateStatus(OrderStatus.Preparing, actorId);
                    orderRepository.Update(order);
                    await orderStatusHistoryRepository.AddAsync(
                        OrderStatusHistoryEntity.Create(order.Id, OrderStatus.Pending, OrderStatus.Preparing, actorId, "RobotPickStarted"),
                        cancellationToken);
                    notifyPreparingStudentId = order.CreatedBy;   // báo Học Sinh "đang chuẩn bị" (sau commit)
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Báo Học Sinh: robot bắt đầu chuẩn bị đơn. Best-effort.
            if (notifyPreparingStudentId is Guid studentId)
            {
                try
                {
                    await businessNotificationService.NotifyAsync(
                        NotificationTemplateKeys.OrderPreparing,
                        studentId,
                        request.OrderId,
                        new Dictionary<string, string> { ["referenceId"] = request.OrderId.ToString() },
                        new { OrderId = request.OrderId },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to notify student of preparing for order {OrderId}", request.OrderId);
                }

                // Bắn real-time đổi status cho Học Sinh + Staff (FE cập nhật badge live).
                await orderStatusNotifier.BroadcastAsync(
                    request.OrderId, studentId, (int)OrderStatus.Preparing, "Preparing", cancellationToken);
            }

            // Báo Staff: robot vừa đặt xong MÓN CUỐI (khay đã ráp đủ) -> sẵn sàng để staff quét lên kệ. Best-effort.
            if (eventType == RobotEventType.PlaceCompleted
                && await IsOrderFullyAssembledAsync(request.OrderId, job.Id, cancellationToken))
            {
                try
                {
                    await servingFailureNotifier.NotifyAllStaffAsync(
                        NotificationTemplateKeys.OrderAssembledStaff,
                        request.OrderId,
                        new Dictionary<string, string>
                        {
                            ["referenceId"] = request.OrderId.ToString(),
                            ["orderId"] = request.OrderId.ToString()
                        },
                        new { OrderId = request.OrderId },
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to notify staff of assembled order {OrderId}", request.OrderId);
                }
            }

            // Executor TỰ báo Failed (verify-fail / hết hàng / HoldForStaff) -> báo staff (mobile).
            //   (Lỗi "câm" = executor chết -> Watchdog lo, KHÔNG qua đây.)
            if (eventType == RobotEventType.Error)
            {
                await servingFailureNotifier.NotifyStaffAsync(
                    request.OrderId,
                    request.Message ?? "Robot báo lỗi khi phục vụ.",
                    cancellationToken);

                // Báo Học Sinh (chủ đơn): đơn đang được xử lý lại. Best-effort — notify lỗi KHÔNG làm hỏng report.
                try
                {
                    var failedOrder = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                    if (failedOrder is not null)
                    {
                        await businessNotificationService.NotifyAsync(
                            NotificationTemplateKeys.OrderServingIssue,
                            failedOrder.CreatedBy,
                            failedOrder.Id,
                            new Dictionary<string, string> { ["referenceId"] = failedOrder.Id.ToString() },
                            new { OrderId = failedOrder.Id },
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to notify student of serving issue for order {OrderId}", request.OrderId);
                }
            }

            // Forward xuống Unity (digital twin): demo = echo lại report của chính Unity;
            // robot thật = Unity mirror theo tay thật. Enrich DishId -> tên để Unity hiển thị.
            var vizType = MapVizType(eventType);
            if (vizType is not null)
            {
                string? dishName = null;
                if (request.DishId is Guid dishId)
                {
                    var dish = await dishRepository.GetByIdAsync(dishId, cancellationToken);
                    dishName = dish?.Name;
                }

                await servingVisualizer.PublishAsync(
                    new ServingVisualEvent(
                        vizType, request.OrderId,
                        JobId: job.Id,
                        Station: request.Station,
                        DishId: request.DishId,
                        DishName: dishName,
                        Message: request.Message),
                    cancellationToken);
            }

            return Result.Success(
                new ReportServingStatusResponse(request.OrderId, state, job.Status.ToString()),
                "Status recorded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording robot status for order {OrderId}", request.OrderId);
            return Result.Failure<ReportServingStatusResponse>(
                Error.ServerError,
                "An error occurred while recording the robot status.");
        }
    }

    private static RobotEventType MapEventType(string state) =>
        state.ToLowerInvariant() switch
        {
            "connected" => RobotEventType.Connected,
            "disconnected" => RobotEventType.Disconnected,
            "jobreceived" or "received" => RobotEventType.JobReceived,
            "pickstarted" or "assembling" => RobotEventType.PickStarted,
            "pickcompleted" => RobotEventType.PickCompleted,
            "placecompleted" => RobotEventType.PlaceCompleted,
            "recovered" => RobotEventType.Recovered,
            "estop" or "emergencystop" => RobotEventType.EmergencyStop,
            "failed" or "error" => RobotEventType.Error,
            _ => RobotEventType.JobReceived
        };

    // RobotEventType -> loại event Unity. null = không thuộc luồng phục vụ 1 đơn (bỏ qua).
    private static string? MapVizType(RobotEventType t) =>
        t switch
        {
            RobotEventType.JobReceived => "jobReceived",
            RobotEventType.PickStarted => "pickStarted",
            RobotEventType.PickCompleted => "pickCompleted",
            RobotEventType.PlaceCompleted => "placeCompleted",
            RobotEventType.Error => "servingFailed",
            _ => null   // Connected/Disconnected/EmergencyStop/Recovered: mức trạm, không phải mức đơn
        };

    // Đơn đã RÁP ĐỦ MÓN chưa: tất cả DishId của đơn đều có log PlaceCompleted (đọc RobotEventLog).
    //   Dùng để báo Staff "cánh tay đã gắp xong" đúng lúc món cuối vừa đặt.
    private async Task<bool> IsOrderFullyAssembledAsync(Guid orderId, Guid servingJobId, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct, o => o.OrderItems);
        if (order is null) return false;

        // #10: cần ĐỦ SỐ LƯỢNG mỗi món (Σ Quantity per DishId), không chỉ "có mặt".
        var neededByDish = order.OrderItems
            .GroupBy(i => i.DishId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (neededByDish.Count == 0) return false;

        var placedLogs = await robotEventLogRepository.FindListAsync(
            x => x.ServingJobId == servingJobId
                 && x.EventType == RobotEventType.PlaceCompleted
                 && x.DishId != null,
            ct);
        var placedByDish = placedLogs
            .GroupBy(x => x.DishId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // đủ khi MỌI món có số tô đã đặt >= số cần (qty=1 -> giống "có mặt" cũ).
        return neededByDish.All(kv =>
            placedByDish.TryGetValue(kv.Key, out var placed) && placed >= kv.Value);
    }
}
