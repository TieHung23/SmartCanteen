using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.SlotConfigurationAdmin.UpdateSlotConfiguration;

internal sealed class UpdateSlotConfigurationCommandHandler(
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<UpdateSlotConfigurationCommandHandler> logger
) : ICommandHandler<UpdateSlotConfigurationCommand, UpdateSlotConfigurationResponse>
{
    public async Task<Result<UpdateSlotConfigurationResponse>> Handle(
        UpdateSlotConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var laneCode = request.LaneCode.Trim();
            if (laneCode.Length == 0 || request.Capacity <= 0)
            {
                return Result.Failure<UpdateSlotConfigurationResponse>(
                    Error.InvalidValue, "LaneCode is required and Capacity must be > 0.");
            }

            var config = await slotConfigurationRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (config is null)
            {
                return Result.Failure<UpdateSlotConfigurationResponse>(
                    Error.SlotConfigurationNotFound, "Slot configuration was not found.");
            }

            var dish = await dishRepository.FindSingleAsync(
                x => x.Id == request.DishId && !x.IsDeleted, cancellationToken);
            if (dish is null)
            {
                return Result.Failure<UpdateSlotConfigurationResponse>(
                    Error.DishNotFound, "Dish was not found.");
            }

            string? armCode = null;
            if (request.RobotArmId is Guid armId)
            {
                var arm = await robotArmRepository.FindSingleAsync(
                    x => x.Id == armId && !x.IsDeleted, cancellationToken);
                if (arm is null)
                {
                    return Result.Failure<UpdateSlotConfigurationResponse>(
                        Error.RobotArmNotFound, "Robot arm was not found.");
                }
                armCode = arm.Code;
            }

            if (!LaneCatalog.IsValidLane(laneCode, armCode))
            {
                return Result.Failure<UpdateSlotConfigurationResponse>(
                    Error.InvalidValue,
                    armCode is null
                        ? "LaneCode must look like '<station>_L1'..'_L3'."
                        : $"LaneCode must be one of {string.Join(", ", LaneCatalog.ForStation(armCode))}.");
            }

            // lane mới không được đụng lane của cấu hình khác trong cùng session
            var laneTaken = await slotConfigurationRepository.FindSingleAsync(
                x => x.SessionId == config.SessionId
                     && x.Id != config.Id
                     && !x.IsDeleted
                     && x.LaneCode.ToLower() == laneCode.ToLower(),
                cancellationToken);
            if (laneTaken is not null)
            {
                return Result.Failure<UpdateSlotConfigurationResponse>(
                    Error.CodeAlreadyExists,
                    $"Lane '{laneCode}' is already assigned in this session.");
            }

            config.Update(request.DishId, laneCode, request.Capacity, request.RobotArmId, currentUserService.UserId);
            slotConfigurationRepository.Update(config);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new UpdateSlotConfigurationResponse(
                    config.Id, config.SessionId, config.DishId, config.LaneCode,
                    config.Capacity, config.RobotArmId),
                "Slot configuration updated.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating slot configuration {Id}", request.Id);
            return Result.Failure<UpdateSlotConfigurationResponse>(
                Error.ServerError, "An error occurred while updating the slot configuration.");
        }
    }
}
