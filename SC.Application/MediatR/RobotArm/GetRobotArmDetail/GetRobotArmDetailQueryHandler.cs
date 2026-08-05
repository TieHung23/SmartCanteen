using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.RobotArm.GetRobotArmDetail;

internal sealed class GetRobotArmDetailQueryHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetRobotArmDetailQueryHandler> logger
) : IQueryHandler<GetRobotArmDetailQuery, GetRobotArmDetailResponse>
{
    public async Task<Result<GetRobotArmDetailResponse>> Handle(
        GetRobotArmDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arm = await robotArmRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (arm is null)
            {
                return Result.Failure<GetRobotArmDetailResponse>(
                    Error.RobotArmNotFound, "Robot arm was not found.");
            }

            // Các lane tay này phục vụ (mọi ca), kèm tên món cho FE đọc thay vì Guid.
            var configs = await slotConfigurationRepository.FindListAsync(
                x => x.RobotArmId == arm.Id && !x.IsDeleted, cancellationToken);

            var dishIds = configs.Select(x => x.DishId).Distinct().ToList();
            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNames = dishes.ToDictionary(x => x.Id, x => x.Name);

            var lanes = configs
                .OrderBy(x => x.LaneCode)
                .Select(x => new ArmLaneDto(
                    x.Id, x.SessionId, x.LaneCode, x.DishId,
                    dishNames.TryGetValue(x.DishId, out var dn) ? dn : null,
                    x.Capacity))
                .ToList();

            return Result.Success(
                new GetRobotArmDetailResponse(
                    arm.Id, arm.Code, arm.Name, arm.IpAddress, arm.StationIndex,
                    arm.Status.ToString(), arm.LastHeartbeatUtc,
                    arm.CreatedAtUtc, arm.UpdatedAtUtc, lanes),
                "Robot arm detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting robot arm detail {Id}", request.Id);
            return Result.Failure<GetRobotArmDetailResponse>(
                Error.ServerError, "An error occurred while getting the robot arm detail.");
        }
    }
}
