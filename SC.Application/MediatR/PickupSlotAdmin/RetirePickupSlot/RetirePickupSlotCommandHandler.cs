using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.PickupSlot.Enum;
using PickupSlotEntity = SC.Domain.Domain.PickupSlot.Entity.PickupSlot;

namespace SC.Application.MediatR.PickupSlotAdmin.RetirePickupSlot;

internal sealed class RetirePickupSlotCommandHandler(
    IGenericRepository<PickupSlotEntity, Guid> pickupSlotRepository,
    IUnitOfWork unitOfWork,
    ILogger<RetirePickupSlotCommandHandler> logger
) : ICommandHandler<RetirePickupSlotCommand, RetirePickupSlotResponse>
{
    public async Task<Result<RetirePickupSlotResponse>> Handle(
        RetirePickupSlotCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var slot = await pickupSlotRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (slot is null)
            {
                return Result.Failure<RetirePickupSlotResponse>(
                    Error.PickupSlotNotFound, "Không tìm thấy ô kệ.");
            }

            if (slot.Status != PickupSlotStatus.Empty)
            {
                return Result.Failure<RetirePickupSlotResponse>(
                    Error.ResourceBusy, "Ô kệ đang có đồ. Hãy dọn (force-clear) trước.");
            }

            slot.SoftDelete();
            pickupSlotRepository.Update(slot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new RetirePickupSlotResponse(slot.Id, slot.Code), "Đã ngừng dùng ô kệ.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retiring pickup slot {Id}", request.Id);
            return Result.Failure<RetirePickupSlotResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi ngừng dùng ô kệ.");
        }
    }
}
