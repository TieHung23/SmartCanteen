using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.SharedKernel.ValueObjects;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Dish.UpdateDish;

internal class UpdateDishCommandHandler(
    IRepositoryBase<DishAggregateRoot, Guid> dishRepository,
    IRepositoryBase<CategoryAggregateRoot, Guid> categoryRepository,
    IRepositoryBase<MealAggregateRoot, Guid> mealRepository,
    ICurrentUserService currentUserService,
    ILogger<UpdateDishCommandHandler> logger
) : ICommandHandler<UpdateDishCommand, UpdateDishResponse>
{
    public async Task<Result<UpdateDishResponse>> Handle(
        UpdateDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dish = await dishRepository.FindByIdAsync(request.Id, cancellationToken);
            if (dish is null || dish.IsDeleted)
            {
                return Result.Failure<UpdateDishResponse>(Error.NullValue, "Dish not found.");
            }

            var category = await categoryRepository.FindByIdAsync(request.CategoryId, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<UpdateDishResponse>(Error.NullValue, "Category not found.");
            }

            var meal = await mealRepository.FindByIdAsync(request.MealId, cancellationToken);
            if (meal is null || meal.IsDeleted || !meal.IsActive)
            {
                return Result.Failure<UpdateDishResponse>(Error.NullValue, "Meal not found.");
            }

            dish.Update(
                request.Name.Trim(),
                request.Description.Trim(),
                Money.Create(request.Price, request.Currency),
                request.StockQuantity,
                request.MealId,
                request.CategoryId,
                request.IsActive,
                currentUserService.UserId);

            var updateResult = await dishRepository.UpdateAsync(dish);
            if (updateResult.IsFailure)
            {
                return Result.Failure<UpdateDishResponse>(
                    updateResult.Error ?? Error.ServerError,
                    updateResult.Message);
            }

            var response = new UpdateDishResponse
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

            return Result.Success(response, "Dish updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating dish {DishId}", request.Id);
            return Result.Failure<UpdateDishResponse>(
                Error.ServerError,
                "An error occurred while updating dish.");
        }
    }
}
