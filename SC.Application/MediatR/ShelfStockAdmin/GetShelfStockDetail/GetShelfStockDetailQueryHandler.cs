using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using ShelfStockEntity = SC.Domain.Domain.ShelfStock.Entity.ShelfStock;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.ShelfStockAdmin.GetShelfStockDetail;

internal sealed class GetShelfStockDetailQueryHandler(
    IGenericRepository<ShelfStockEntity, Guid> shelfStockRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetShelfStockDetailQueryHandler> logger
) : IQueryHandler<GetShelfStockDetailQuery, GetShelfStockDetailResponse>
{
    public async Task<Result<GetShelfStockDetailResponse>> Handle(
        GetShelfStockDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stock = await shelfStockRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (stock is null)
            {
                return Result.Failure<GetShelfStockDetailResponse>(
                    Error.ShelfStockNotFound, "Shelf stock record was not found.");
            }

            // Enrich tên ca/món/lane cho FE đọc (không lọc IsDeleted để vẫn hiện tên).
            var session = await sessionRepository.FindSingleAsync(
                x => x.Id == stock.SessionId, cancellationToken);
            var dish = await dishRepository.FindSingleAsync(
                x => x.Id == stock.DishId, cancellationToken);
            SlotConfigurationEntity? cfg = stock.SlotConfigurationId is Guid cid
                ? await slotConfigurationRepository.FindSingleAsync(x => x.Id == cid, cancellationToken)
                : null;

            return Result.Success(
                new GetShelfStockDetailResponse(
                    stock.Id, stock.SessionId, session?.Name, stock.DishId, dish?.Name,
                    stock.SlotConfigurationId, cfg?.LaneCode, stock.Quantity,
                    stock.CreatedAtUtc, stock.UpdatedAtUtc),
                "Shelf stock detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting shelf stock detail {Id}", request.Id);
            return Result.Failure<GetShelfStockDetailResponse>(
                Error.ServerError, "An error occurred while getting the shelf stock detail.");
        }
    }
}
