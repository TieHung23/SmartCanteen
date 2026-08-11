using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.SlotConfigurationAdmin.GetSlotConfigurationDetail;

internal sealed class GetSlotConfigurationDetailQueryHandler(
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    ILogger<GetSlotConfigurationDetailQueryHandler> logger
) : IQueryHandler<GetSlotConfigurationDetailQuery, GetSlotConfigurationDetailResponse>
{
    public async Task<Result<GetSlotConfigurationDetailResponse>> Handle(
        GetSlotConfigurationDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cfg = await slotConfigurationRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (cfg is null)
            {
                return Result.Failure<GetSlotConfigurationDetailResponse>(
                    Error.SlotConfigurationNotFound, "Slot configuration was not found.");
            }

            // Enrich tên ca/món/tay cho FE (không lọc IsDeleted để vẫn hiện tên dù bản ghi gốc đã xoá mềm).
            var session = await sessionRepository.FindSingleAsync(
                x => x.Id == cfg.SessionId, cancellationToken);
            var dish = await dishRepository.FindSingleAsync(
                x => x.Id == cfg.DishId, cancellationToken);
            RobotArmEntity? arm = cfg.RobotArmId is Guid armId
                ? await robotArmRepository.FindSingleAsync(x => x.Id == armId, cancellationToken)
                : null;

            return Result.Success(
                new GetSlotConfigurationDetailResponse(
                    cfg.Id, cfg.SessionId, session?.Name, cfg.DishId, dish?.Name,
                    cfg.LaneCode, cfg.Capacity,
                    cfg.RobotArmId, arm?.Code, arm?.Name,
                    cfg.CreatedAtUtc, cfg.UpdatedAtUtc),
                "Slot configuration detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting slot configuration detail {Id}", request.Id);
            return Result.Failure<GetSlotConfigurationDetailResponse>(
                Error.ServerError, "An error occurred while getting the slot configuration detail.");
        }
    }
}
