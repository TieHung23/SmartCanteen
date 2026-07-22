using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.PickupSlot.Enum;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;

namespace SC.Application.MediatR.PickupSlotAdmin.GetAllPickupSlots;

internal sealed class GetAllPickupSlotsQueryHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    ILogger<GetAllPickupSlotsQueryHandler> logger
) : IQueryHandler<GetAllPickupSlotsQuery, GetAllPickupSlotsResponse>
{
    public async Task<Result<GetAllPickupSlotsResponse>> Handle(
        GetAllPickupSlotsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slots = await pickupSlotRepository.FindListAsync(x => !x.IsDeleted, cancellationToken);

            var dtos = slots
                .OrderBy(x => x.Code)
                .Select(x => new PickupSlotDto(
                    x.Id, x.Code, x.Status.ToString(), x.OrderId, x.UpdatedAtUtc))
                .ToList();

            return Result.Success(new GetAllPickupSlotsResponse(
                slots.Count(x => x.Status == PickupSlotStatus.Empty),
                slots.Count(x => x.Status == PickupSlotStatus.Occupied),
                dtos), "Pickup slots retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing pickup slots");
            return Result.Failure<GetAllPickupSlotsResponse>(
                Error.ServerError, "An error occurred while listing pickup slots.");
        }
    }
}
