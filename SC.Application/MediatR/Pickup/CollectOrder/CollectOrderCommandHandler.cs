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
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Pickup.CollectOrder;

internal sealed class CollectOrderCommandHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingVisualizer servingVisualizer,
    ILogger<CollectOrderCommandHandler> logger
) : ICommandHandler<CollectOrderCommand, CollectOrderResponse>
{
    public async Task<Result<CollectOrderResponse>> Handle(
        CollectOrderCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            // Dò ngược OrderId -> ô kệ đang giữ khay của order
            var slot = await pickupSlotRepository
                .FindSingleAsync(x => x.OrderId == request.OrderId
                                   && x.Status != PickupSlotStatus.Empty,
                    cancellationToken);

            var jobs = await servingJobRepository
                .FindListAsync(x => x.OrderId == request.OrderId
                                   && x.Status != ServingJobStatus.Cancelled
                                   && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
            var job = jobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (job is null)
                return Result.Failure<CollectOrderResponse>(Error.ServingJobNotFound, "No active serving job for this order.");

            // Khay ĐÃ được trả về pool lúc staff lên kệ (AssignPickupSlot). Collect chỉ dọn ô.
            // (Không release khay ở đây: khay có thể đã tái dùng cho đơn khác -> release là sai.)
            string? slotCode = null;
            if (slot is not null)
            {
                slotCode = slot.Code;
                slot.Clear(actorId);
                pickupSlotRepository.Update(slot);
            }

            job.MarkCollected(actorId);
            servingJobRepository.Update(job);

            var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order is not null && order.Status != OrderStatus.Completed)
            {
                var from = order.Status;
                order.UpdateStatus(OrderStatus.Completed, actorId);
                orderRepository.Update(order);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(order.Id, from, OrderStatus.Completed, actorId, "Collected"),
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Unity: khách đã lấy -> avatar học sinh nhận đồ, ô nhận mở/dọn. Best-effort.
            await servingVisualizer.PublishAsync(
                new ServingVisualEvent(
                    "collected", request.OrderId,
                    JobId: job.Id,
                    PickupSlotCode: slotCode),
                cancellationToken);

            return Result.Success(
                new CollectOrderResponse(request.OrderId, slotCode),
                "Order collected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error collecting order {OrderId}", request.OrderId);
            return Result.Failure<CollectOrderResponse>(
                Error.ServerError,
                "An error occurred while collecting the order.");
        }
    }
}
