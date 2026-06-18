using Microsoft.EntityFrameworkCore;
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
    ICurrentUserService currentUserService
) : ICommandHandler<AssignPickupSlotCommand, AssignPickupSlotResponse>
{
    public async Task<Result<AssignPickupSlotResponse>> Handle(
        AssignPickupSlotCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        var slot = await pickupSlotRepository
            .GetQueryable(x => x.Code == request.SlotCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (slot is null)
            return Result.Failure<AssignPickupSlotResponse>(Error.NullValue, "Pickup slot was not found.");
        if (slot.Status != PickupSlotStatus.Empty)
            return Result.Failure<AssignPickupSlotResponse>(Error.InvalidValue, "Pickup slot is not empty.");

        var tray = await trayRepository
            .GetQueryable(x => x.Code == request.TrayCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (tray is null)
            return Result.Failure<AssignPickupSlotResponse>(Error.NullValue, "Tray was not found.");

        var job = await servingJobRepository
            .GetQueryable(x => x.OrderId == request.OrderId
                               && x.Status != ServingJobStatus.Cancelled
                               && x.Status != ServingJobStatus.Collected)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
            return Result.Failure<AssignPickupSlotResponse>(Error.NullValue, "No active serving job for this order.");

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
}
