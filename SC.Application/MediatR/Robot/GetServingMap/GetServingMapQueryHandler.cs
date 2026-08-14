using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Robot.GetServingMap;

internal sealed class GetServingMapQueryHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetServingMapQueryHandler> logger
) : IQueryHandler<GetServingMapQuery, GetServingMapResponse>
{
    public async Task<Result<GetServingMapResponse>> Handle(
        GetServingMapQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ca ĐANG MỞ (AvailableFrom <= now <= AvailableTo) — cùng logic filter với PullNextJob.
            var now = DateTimeOffset.UtcNow;
            var activeSessions = await sessionRepository.FindListAsync(
                x => !x.IsDeleted && x.AvailableFrom <= now && now <= x.AvailableTo,
                cancellationToken);
            var activeSessionIds = activeSessions.Select(x => x.Id).ToHashSet();
            if (activeSessionIds.Count == 0)
            {
                return Result.Success(
                    new GetServingMapResponse(Array.Empty<ServingMapLane>()),
                    "Không có ca đang mở.");
            }

            var configs = await slotConfigurationRepository.FindListAsync(
                x => activeSessionIds.Contains(x.SessionId) && !x.IsDeleted,
                cancellationToken);

            var dishIds = configs.Select(x => x.DishId).Distinct().ToList();
            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNameById = dishes.ToDictionary(x => x.Id, x => x.Name);

            // 1 laneCode trong 1 ca là duy nhất; nếu nhiều ca mở chồng -> lane sau ghi đè (hiếm).
            var lanes = configs
                .Select(c => new ServingMapLane(
                    c.LaneCode,
                    dishNameById.TryGetValue(c.DishId, out var name) ? name : null))
                .ToList();

            return Result.Success(new GetServingMapResponse(lanes), "Bản đồ phục vụ của ca đang mở.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building serving map");
            return Result.Failure<GetServingMapResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi dựng bản đồ phục vụ.");
        }
    }
}
