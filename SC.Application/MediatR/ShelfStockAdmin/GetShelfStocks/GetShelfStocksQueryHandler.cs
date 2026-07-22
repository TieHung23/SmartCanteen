using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using ShelfStockEntity = SC.Domain.Domain.ShelfStock.Entity.ShelfStock;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.ShelfStockAdmin.GetShelfStocks;

internal sealed class GetShelfStocksQueryHandler(
    IGenericRepository<ShelfStockEntity, Guid> shelfStockRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetShelfStocksQueryHandler> logger
) : IQueryHandler<GetShelfStocksQuery, GetShelfStocksResponse>
{
    public async Task<Result<GetShelfStocksResponse>> Handle(
        GetShelfStocksQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stocks = await shelfStockRepository.FindListAsync(
                x => x.SessionId == request.SessionId && !x.IsDeleted, cancellationToken);

            // map laneCode + dishName để staff đọc được (thay vì Guid)
            var configIds = stocks.Where(x => x.SlotConfigurationId.HasValue)
                .Select(x => x.SlotConfigurationId!.Value).Distinct().ToList();
            var configs = configIds.Count == 0
                ? new List<SlotConfigurationEntity>()
                : await slotConfigurationRepository.FindListAsync(
                    x => configIds.Contains(x.Id), cancellationToken);
            var lanes = configs.ToDictionary(x => x.Id, x => x.LaneCode);

            var dishIds = stocks.Select(x => x.DishId).Distinct().ToList();
            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNames = dishes.ToDictionary(x => x.Id, x => x.Name);

            var dtos = stocks
                .OrderBy(x => x.SlotConfigurationId.HasValue
                    && lanes.TryGetValue(x.SlotConfigurationId.Value, out var l) ? l : "")
                .Select(x => new ShelfStockDto(
                    x.Id, x.SessionId, x.DishId,
                    dishNames.TryGetValue(x.DishId, out var dn) ? dn : null,
                    x.SlotConfigurationId,
                    x.SlotConfigurationId.HasValue
                        && lanes.TryGetValue(x.SlotConfigurationId.Value, out var lc) ? lc : null,
                    x.Quantity))
                .ToList();

            return Result.Success(
                new GetShelfStocksResponse(dtos), "Shelf stocks retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing shelf stocks for session {SessionId}", request.SessionId);
            return Result.Failure<GetShelfStocksResponse>(
                Error.ServerError, "An error occurred while listing shelf stocks.");
        }
    }
}
