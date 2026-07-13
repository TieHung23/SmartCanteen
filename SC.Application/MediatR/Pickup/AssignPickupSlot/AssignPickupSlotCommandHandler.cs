using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
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
    IServingJobNotifier servingJobNotifier,
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

            // Chỉ job ĐÃ RÁP XONG mới được lên kệ (Queued/Pushed = robot chưa làm; Failed = đang chờ xử lý)
            if (job.Status != ServingJobStatus.Assembling)
                return Result.Failure<AssignPickupSlotResponse>(
                    Error.ServingJobNotReady,
                    $"Serving job is '{job.Status}'; only assembled jobs can be shelved.");

            // Khay quét phải ĐÚNG khay job này đang dùng (chặn quét nhầm khay A/khay B).
            // job lấy theo OrderId nên check này = "đơn này ↔ khay này" khớp cả 2 chiều.
            if (job.TrayId != tray.Id)
                return Result.Failure<AssignPickupSlotResponse>(
                    Error.TrayMismatch,
                    $"Tray '{tray.Code}' is not the tray assigned to this order.");

            slot.Assign(request.OrderId, tray.Id, actorId);
            pickupSlotRepository.Update(slot);

            // Kraft bowls: staff bê đồ lên ô kệ -> KHAY RỖNG, trả về pool NGAY (không nằm trên ô).
            tray.Release(actorId);
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

            // Khay vừa Release về pool -> ping đánh thức robot phục vụ đơn đang chờ khay (best-effort).
            try
            {
                await servingJobNotifier.PingNewJobAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ping robots after freeing tray (slot {SlotCode})", request.SlotCode);
            }

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
