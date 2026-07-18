using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
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

namespace SC.Application.MediatR.Robot.ReportServingStatus;

internal sealed class ReportServingStatusCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
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
                    message: request.Message ?? state),
                cancellationToken);

            // Đồng bộ Order status khi robot bắt đầu ráp
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
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
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
}
