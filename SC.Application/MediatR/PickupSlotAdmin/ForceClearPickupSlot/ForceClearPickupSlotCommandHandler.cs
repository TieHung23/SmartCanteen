using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Visualization;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.PickupSlot.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.PickupSlotAdmin.ForceClearPickupSlot;

internal sealed class ForceClearPickupSlotCommandHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingVisualizer servingVisualizer,
    ILogger<ForceClearPickupSlotCommandHandler> logger
) : ICommandHandler<ForceClearPickupSlotCommand, ForceClearPickupSlotResponse>
{
    public async Task<Result<ForceClearPickupSlotResponse>> Handle(
        ForceClearPickupSlotCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var slot = await pickupSlotRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (slot is null)
            {
                return Result.Failure<ForceClearPickupSlotResponse>(
                    Error.PickupSlotNotFound, "Pickup slot was not found.");
            }

            if (slot.Status == PickupSlotStatus.Empty)
            {
                return Result.Success(
                    new ForceClearPickupSlotResponse(slot.Id, slot.Code, null),
                    "Slot is already empty.");
            }

            var orderId = slot.OrderId;
            slot.Clear(actorId);
            pickupSlotRepository.Update(slot);

            Guid? expiredOrderId = null;
            if (orderId is Guid oid)
            {
                // đơn no-show -> Expired (chỉ khi đang chờ lấy) + đóng job
                var order = await orderRepository.GetByIdAsync(oid, cancellationToken);
                if (order is not null && order.Status == OrderStatus.ReadyForPickup)
                {
                    order.UpdateStatus(OrderStatus.Expired, actorId);
                    orderRepository.Update(order);
                    await orderStatusHistoryRepository.AddAsync(
                        OrderStatusHistoryEntity.Create(
                            order.Id, OrderStatus.ReadyForPickup, OrderStatus.Expired,
                            actorId, "PickupExpiredForceClear"),
                        cancellationToken);
                    expiredOrderId = order.Id;
                }

                var jobs = await servingJobRepository.FindListAsync(
                    x => x.OrderId == oid
                         && x.Status != ServingJobStatus.Cancelled
                         && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
                var job = jobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
                if (job is not null)
                {
                    job.Cancel(actorId);
                    servingJobRepository.Update(job);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Unity: ô bị dọn ép (khách no-show) -> dọn khay ở ô, đơn hết hạn. Best-effort.
            if (orderId is Guid clearedOrderId)
            {
                await servingVisualizer.PublishAsync(
                    new ServingVisualEvent(
                        "expired", clearedOrderId,
                        PickupSlotCode: slot.Code,
                        Message: expiredOrderId is null ? "Slot cleared." : "No-show; order expired."),
                    cancellationToken);
            }

            return Result.Success(
                new ForceClearPickupSlotResponse(slot.Id, slot.Code, expiredOrderId),
                expiredOrderId is null
                    ? "Slot cleared."
                    : "Slot cleared; order marked Expired (no-show).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error force-clearing pickup slot {Id}", request.Id);
            return Result.Failure<ForceClearPickupSlotResponse>(
                Error.ServerError, "An error occurred while clearing the pickup slot.");
        }
    }
}
