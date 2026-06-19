using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.PickupSlot.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Pickup.AssignPickupSlot;

internal sealed class AssignPickupSlotCommandHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<AssignPickupSlotCommandHandler> logger
) : ICommandHandler<AssignPickupSlotCommand, AssignPickupSlotResponse>
{
    public async Task<Result<AssignPickupSlotResponse>> Handle(
        AssignPickupSlotCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var slot = await pickupSlotRepository
                .FindSingleAsync(x => x.Code == request.SlotCode, cancellationToken);
            if (slot is null)
                return Result.Failure<AssignPickupSlotResponse>(Error.PickupSlotNotFound, "Pickup slot was not found.");
            if (slot.Status != PickupSlotStatus.Empty)
                return Result.Failure<AssignPickupSlotResponse>(Error.PickupSlotNotAvailable, "Pickup slot is not empty.");

            var tray = await trayRepository
                .FindSingleAsync(x => x.Code == request.TrayCode, cancellationToken);
            if (tray is null)
                return Result.Failure<AssignPickupSlotResponse>(Error.TrayNotFound, "Tray was not found.");

            var jobs = await servingJobRepository
                .FindListAsync(x => x.OrderId == request.OrderId
                                   && x.Status != ServingJobStatus.Cancelled
                                   && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
            var job = jobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            if (job is null)
                return Result.Failure<AssignPickupSlotResponse>(Error.ServingJobNotFound, "No active serving job for this order.");

            slot.Assign(request.OrderId, tray.Id, actorId);
            pickupSlotRepository.Update(slot);

            tray.MarkAtSlot(actorId);
            trayRepository.Update(tray);

            job.MarkOnShelf(slot.Id, actorId);
            servingJobRepository.Update(job);

            var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order is not null && order.Status != OrderStatus.ReadyForPickup)
            {
                var from = order.Status;
                order.UpdateStatus(OrderStatus.ReadyForPickup, actorId);
                orderRepository.Update(order);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(order.Id, from, OrderStatus.ReadyForPickup, actorId, "AssignedToPickupSlot"),
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new AssignPickupSlotResponse(slot.Id, slot.Code, request.OrderId, tray.Id),
                "Order assigned to pickup slot.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning pickup slot {SlotCode} for order {OrderId}", request.SlotCode, request.OrderId);
            return Result.Failure<AssignPickupSlotResponse>(
                Error.ServerError,
                "An error occurred while assigning the pickup slot.");
        }
    }
}
