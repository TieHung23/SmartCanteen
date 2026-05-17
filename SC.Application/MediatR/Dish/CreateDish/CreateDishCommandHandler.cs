using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.SharedKernel.ValueObjects;
using CategoryAggregateRoot = SC.Domain.Domain.Category.AggregateRoot.Category;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Dish.CreateDish;

internal class CreateDishCommandHandler(
    IRepositoryBase<DishAggregateRoot, Guid> dishRepository,
    IRepositoryBase<CategoryAggregateRoot, Guid> categoryRepository,
    IRepositoryBase<MealAggregateRoot, Guid> mealRepository,
    ICurrentUserService currentUserService,
    ILogger<CreateDishCommandHandler> logger
) : ICommandHandler<CreateDishCommand, CreateDishResponse>
{
    public async Task<Result<CreateDishResponse>> Handle(
        CreateDishCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.FindByIdAsync(request.CategoryId, cancellationToken);
            if (category is null || category.IsDeleted)
            {
                return Result.Failure<CreateDishResponse>(Error.NullValue, "Category not found.");
            }

            var meal = await mealRepository.FindByIdAsync(request.MealId, cancellationToken);
            if (meal is null || meal.IsDeleted || !meal.IsActive)
            {
                return Result.Failure<CreateDishResponse>(Error.NullValue, "Meal not found.");
            }

            var dish = DishAggregateRoot.Create(
                request.Name.Trim(),
                request.Description.Trim(),
                Money.Create(request.Price, request.Currency),
                request.StockQuantity,
                request.MealId,
                request.CategoryId,
                currentUserService.UserId);

            var createResult = await dishRepository.AddAsync(dish);
            if (createResult.IsFailure)
            {
                return Result.Failure<CreateDishResponse>(
                    createResult.Error ?? Error.ServerError,
                    createResult.Message);
            }

            var response = new CreateDishResponse
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

            return Result.Success(response, "Dish created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating dish");
            return Result.Failure<CreateDishResponse>(
                Error.ServerError,
                "An error occurred while creating dish.");
        }
    }
}
