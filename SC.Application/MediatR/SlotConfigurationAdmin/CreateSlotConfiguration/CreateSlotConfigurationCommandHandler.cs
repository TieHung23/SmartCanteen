using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.SlotConfigurationAdmin.CreateSlotConfiguration;

internal sealed class CreateSlotConfigurationCommandHandler(
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateSlotConfigurationCommandHandler> logger
) : ICommandHandler<CreateSlotConfigurationCommand, CreateSlotConfigurationResponse>
{
    public async Task<Result<CreateSlotConfigurationResponse>> Handle(
        CreateSlotConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var laneCode = request.LaneCode.ToString();   // enum -> "S1_L1"
            if (request.Capacity <= 0)
            {
                return Result.Failure<CreateSlotConfigurationResponse>(
                    Error.InvalidValue, "Capacity must be > 0.");
            }

            var session = await sessionRepository.FindSingleAsync(
                x => x.Id == request.SessionId && !x.IsDeleted, cancellationToken);
            if (session is null)
            {
                return Result.Failure<CreateSlotConfigurationResponse>(
                    Error.SessionNotFound, "Session was not found.");
            }

            var dish = await dishRepository.FindSingleAsync(
                x => x.Id == request.DishId && !x.IsDeleted, cancellationToken);
            if (dish is null)
            {
                return Result.Failure<CreateSlotConfigurationResponse>(
                    Error.DishNotFound, "Dish was not found.");
            }

            string? armCode = null;
            if (request.RobotArmId is Guid armId)
            {
                var arm = await robotArmRepository.FindSingleAsync(
                    x => x.Id == armId && !x.IsDeleted, cancellationToken);
                if (arm is null)
                {
                    return Result.Failure<CreateSlotConfigurationResponse>(
                        Error.RobotArmNotFound, "Robot arm was not found.");
                }
                armCode = arm.Code;
            }

            // LaneCode phải là lane hợp lệ của trạm ({Code}_L1..L3) — khớp dropdown FE / GET lanes
            if (!LaneCatalog.IsValidLane(laneCode, armCode))
            {
                return Result.Failure<CreateSlotConfigurationResponse>(
                    Error.InvalidValue,
                    armCode is null
                        ? "LaneCode must look like '<station>_L1'..'_L3'."
                        : $"LaneCode must be one of {string.Join(", ", LaneCatalog.ForStation(armCode))}.");
            }

            // 1 lane chỉ chứa 1 món trong 1 ca
            var laneTaken = await slotConfigurationRepository.FindSingleAsync(
                x => x.SessionId == request.SessionId
                     && !x.IsDeleted
                     && x.LaneCode.ToLower() == laneCode.ToLower(),
                cancellationToken);
            if (laneTaken is not null)
            {
                return Result.Failure<CreateSlotConfigurationResponse>(
                    Error.CodeAlreadyExists,
                    $"Lane '{laneCode}' is already assigned in this session.");
            }

            var config = SlotConfigurationEntity.Create(
                request.SessionId, request.DishId, laneCode, request.Capacity,
                currentUserService.UserId, request.RobotArmId);

            await slotConfigurationRepository.AddAsync(config, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new CreateSlotConfigurationResponse(
                    config.Id, config.SessionId, config.DishId, config.LaneCode,
                    config.Capacity, config.RobotArmId),
                "Slot configuration created.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating slot configuration");
            return Result.Failure<CreateSlotConfigurationResponse>(
                Error.ServerError, "An error occurred while creating the slot configuration.");
        }
    }
}
