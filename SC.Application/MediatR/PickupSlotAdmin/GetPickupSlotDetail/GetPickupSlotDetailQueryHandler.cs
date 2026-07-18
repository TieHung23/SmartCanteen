using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.PickupSlot.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.PickupSlotAdmin.GetPickupSlotDetail;

internal sealed class GetPickupSlotDetailQueryHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ILogger<GetPickupSlotDetailQueryHandler> logger
) : IQueryHandler<GetPickupSlotDetailQuery, GetPickupSlotDetailResponse>
{
    private const int HistoryTake = 10;

    public async Task<Result<GetPickupSlotDetailResponse>> Handle(
        GetPickupSlotDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slot = await pickupSlotRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (slot is null)
            {
                return Result.Failure<GetPickupSlotDetailResponse>(
                    Error.PickupSlotNotFound, "Pickup slot was not found.");
            }

            // Mọi job từng được đặt vào ô này
            var jobs = await servingJobRepository.FindListAsync(
                x => x.PickupSlotId == slot.Id, cancellationToken);
            var recent = jobs.OrderByDescending(x => x.CreatedAtUtc).Take(HistoryTake).ToList();

            var orderIds = recent.Select(x => x.OrderId).ToList();
            if (slot.OrderId is Guid oid && !orderIds.Contains(oid))
                orderIds.Add(oid);
            orderIds = orderIds.Distinct().ToList();

            var orders = orderIds.Count == 0
                ? new List<OrderAggregateRoot>()
                : await orderRepository.FindListAsync(x => orderIds.Contains(x.Id), cancellationToken);
            var orderStatusById = orders.ToDictionary(x => x.Id, x => x.Status.ToString());

            SlotJobDto Map(ServingJobEntity j) => new(
                j.Id,
                j.OrderId,
                j.Status.ToString(),
                orderStatusById.TryGetValue(j.OrderId, out var os) ? os : null,
                j.CreatedAtUtc,
                j.CompletedAtUtc);

            // Job đang nằm trên ô (chưa được lấy đi)
            var current = recent.FirstOrDefault(x =>
                x.Status != ServingJobStatus.Cancelled &&
                x.Status != ServingJobStatus.Collected);

            // Ô đang giữ đơn: UpdatedAtUtc = lúc staff assign -> tính số phút đã giữ (soi no-show)
            var occupied = slot.Status == PickupSlotStatus.Occupied;
            DateTimeOffset? since = occupied ? slot.UpdatedAtUtc : null;
            int? heldMinutes = since is null
                ? null
                : (int)Math.Max(0, (DateTimeOffset.UtcNow - since.Value).TotalMinutes);

            return Result.Success(
                new GetPickupSlotDetailResponse(
                    slot.Id,
                    slot.Code,
                    slot.Status.ToString(),
                    slot.CreatedAtUtc,
                    slot.UpdatedAtUtc,
                    slot.OrderId,
                    slot.OrderId is Guid o && orderStatusById.TryGetValue(o, out var s) ? s : null,
                    since,
                    heldMinutes,
                    current is null ? null : Map(current),
                    recent.Select(Map).ToList()),
                "Pickup slot detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting pickup slot detail {Id}", request.Id);
            return Result.Failure<GetPickupSlotDetailResponse>(
                Error.ServerError, "An error occurred while getting the pickup slot detail.");
        }
    }
}
