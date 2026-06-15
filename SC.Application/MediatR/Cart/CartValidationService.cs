using Microsoft.EntityFrameworkCore;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Application.MediatR.Cart;

public interface ICartValidationService
{
    Task<Result<ValidatedCart>> ValidateAsync(
        CartData? data,
        CancellationToken cancellationToken = default);
}

public sealed class ValidatedCart
{
    public required MealAggregateRoot Meal { get; init; }
    public required IReadOnlyDictionary<Guid, DishAggregateRoot> Dishes { get; init; }
}

public sealed class CartValidationService(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository)
    : ICartValidationService
{
    public async Task<Result<ValidatedCart>> ValidateAsync(
        CartData? data,
        CancellationToken cancellationToken = default)
    {
        if (data is null)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "Cart data is required.");
        }

        if (data.MealId == Guid.Empty)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "MealId is required.");
        }

        if (data.Items is null || data.Items.Count == 0)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart must contain at least one item.");
        }

        if (data.Items.Any(x => x.DishId == Guid.Empty))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "DishId is required for every cart item.");
        }

        if (data.Items.Any(x => x.Quantity <= 0))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Item quantity must be greater than zero.");
        }

        if (data.Items.GroupBy(x => x.DishId).Any(group => group.Count() > 1))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart cannot contain duplicate dishes.");
        }

        var meal = await mealRepository
            .GetQueryable(x => x.Id == data.MealId)
            .Include(x => x.DishMeals)
            .SingleOrDefaultAsync(cancellationToken);

        if (meal is null || meal.IsDeleted)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "Meal was not found.");
        }

        if (!meal.IsActive)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Meal is not available.");
        }

        if (DateTimeOffset.UtcNow > meal.AvailableForOrder)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "The ordering deadline for this meal has passed.");
        }

        var dishIds = data.Items.Select(x => x.DishId).ToList();
        var dishes = await dishRepository
            .GetQueryable(x => dishIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (dishes.Count != dishIds.Count)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "One or more dishes were not found.");
        }

        if (dishes.Values.Any(x => x.IsDeleted || !x.IsActive))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "One or more dishes are not available.");
        }

        var mealDishes = meal.DishMeals.ToDictionary(x => x.DishId);
        if (dishIds.Any(dishId => !mealDishes.ContainsKey(dishId)))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "One or more dishes do not belong to the selected meal.");
        }

        if (data.Items.Any(item => item.Quantity > mealDishes[item.DishId].Quantity))
        {
            return Result.Failure<ValidatedCart>(
                Error.InsufficientDishStock,
                "One or more dishes do not have enough stock.");
        }

        return Result.Success(
            new ValidatedCart
            {
                Meal = meal,
                Dishes = dishes
            },
            "Cart data is valid.");
    }
}
