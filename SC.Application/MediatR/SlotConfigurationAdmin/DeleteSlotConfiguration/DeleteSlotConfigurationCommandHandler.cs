using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;

namespace SC.Application.MediatR.SlotConfigurationAdmin.DeleteSlotConfiguration;

internal sealed class DeleteSlotConfigurationCommandHandler(
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteSlotConfigurationCommandHandler> logger
) : ICommandHandler<DeleteSlotConfigurationCommand, DeleteSlotConfigurationResponse>
{
    public async Task<Result<DeleteSlotConfigurationResponse>> Handle(
        DeleteSlotConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await slotConfigurationRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (config is null)
            {
                return Result.Failure<DeleteSlotConfigurationResponse>(
                    Error.SlotConfigurationNotFound, "Slot configuration was not found.");
            }

            config.SoftDelete();
            slotConfigurationRepository.Update(config);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new DeleteSlotConfigurationResponse(config.Id, config.LaneCode),
                "Slot configuration removed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting slot configuration {Id}", request.Id);
            return Result.Failure<DeleteSlotConfigurationResponse>(
                Error.ServerError, "An error occurred while deleting the slot configuration.");
        }
    }
}
