using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Dish.GetDishById;

internal class GetDishByIdQueryHandler(
    IRepositoryBase<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetDishByIdQueryHandler> logger
) : IQueryHandler<GetDishByIdQuery, GetDishByIdResponse>
{
    public async Task<Result<GetDishByIdResponse>> Handle(
        GetDishByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.FindByIdAsync(request.Id, cancellationToken);

            if (dish is null)
            {
                return Result.Failure<GetDishByIdResponse>(
                    Error.NullValue,
                    "Dish not found.");
            }

            var response = new GetDishByIdResponse
            {
                Id = dish.Id,
                Name = dish.Name,
                Description = dish.Description,
                Price = dish.Price.Amount,
                Currency = dish.Price.Currency,
                StockQuantity = dish.StockQuantity,
                IsActive = dish.IsActive,
                MealId = dish.MealId,
                CategoryId = dish.CategoryId
            };

            return Result.Success(response, "Dish retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dish by id {DishId}", request.Id);
            return Result.Failure<GetDishByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving dish.");
        }
    }
}
