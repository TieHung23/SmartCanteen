using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.UpdateDishStock;

internal class UpdateDishStockCommandHandler(
    IRepositoryBase<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService,
    ILogger<UpdateDishStockCommandHandler> logger
) : ICommandHandler<UpdateDishStockCommand, UpdateDishStockResponse>
{
    public async Task<Result<UpdateDishStockResponse>> Handle(
        UpdateDishStockCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.FindByIdAsync(request.Id, cancellationToken);
            if (dish is null || dish.IsDeleted)
            {
                return Result.Failure<UpdateDishStockResponse>(Error.NullValue, "Dish not found.");
            }

            dish.UpdateStock(request.StockQuantity, currentUserService.UserId);

            var updateResult = await dishRepository.UpdateAsync(dish);
            if (updateResult.IsFailure)
            {
                return Result.Failure<UpdateDishStockResponse>(
                    updateResult.Error ?? Error.ServerError,
                    updateResult.Message);
            }

            var response = new UpdateDishStockResponse
            {
                Id = dish.Id,
                StockQuantity = dish.StockQuantity,
                IsActive = dish.IsActive
            };

            return Result.Success(response, "Dish stock updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating dish stock {DishId}", request.Id);
            return Result.Failure<UpdateDishStockResponse>(
                Error.ServerError,
                "An error occurred while updating dish stock.");
        }
    }
}
