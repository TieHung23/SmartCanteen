using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using ShelfStockEntity = SC.Domain.Domain.ShelfStock.Entity.ShelfStock;

namespace SC.Application.MediatR.ShelfStockAdmin.RefillShelfStock;

internal sealed class RefillShelfStockCommandHandler(
    IGenericRepository<ShelfStockEntity, Guid> shelfStockRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<RefillShelfStockCommandHandler> logger
) : ICommandHandler<RefillShelfStockCommand, RefillShelfStockResponse>
{
    public async Task<Result<RefillShelfStockResponse>> Handle(
        RefillShelfStockCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Quantity <= 0)
            {
                return Result.Failure<RefillShelfStockResponse>(
                    Error.InvalidValue, "Refill quantity must be > 0.");
            }

            var stock = await shelfStockRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (stock is null)
            {
                return Result.Failure<RefillShelfStockResponse>(
                    Error.ShelfStockNotFound, "Shelf stock record was not found.");
            }

            stock.Refill(request.Quantity, currentUserService.UserId);
            shelfStockRepository.Update(stock);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new RefillShelfStockResponse(stock.Id, stock.DishId, stock.Quantity),
                $"Refilled +{request.Quantity}; current quantity: {stock.Quantity}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refilling shelf stock {Id}", request.Id);
            return Result.Failure<RefillShelfStockResponse>(
                Error.ServerError, "An error occurred while refilling shelf stock.");
        }
    }
}
