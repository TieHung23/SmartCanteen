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
        bool requireCompleteTemplate = true,
        CancellationToken cancellationToken = default);
}

public sealed class ValidatedCart
{
    public required IReadOnlyDictionary<Guid, MealAggregateRoot> Meals { get; init; }
    public required IReadOnlyDictionary<Guid, DishAggregateRoot> Dishes { get; init; }
}

public sealed class CartValidationService(
    IGenericRepository<MealAggregateRoot, Guid> mealRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository)
    : ICartValidationService
{
    public async Task<Result<ValidatedCart>> ValidateAsync(
        CartData? data,
        bool requireCompleteTemplate = true,
        CancellationToken cancellationToken = default)
    {
        if (data is null)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "Cart data is required.");
        }

        if (data.Meals.Count == 0)
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart must contain at least one meal.");
        }

        if (data.Meals.GroupBy(x => x.MealId).Any(group => group.Count() > 1))
        {
            return Result.Failure<ValidatedCart>(
                Error.InvalidValue,
                "Cart cannot contain duplicate meals.");
        }

        foreach (var mealData in data.Meals)
        {
            var basicResult = ValidateBasicMealData(mealData);
            if (basicResult.IsFailure)
            {
                return Result.Failure<ValidatedCart>(
                    basicResult.Error!,
                    basicResult.Message);
            }

            if (mealData.Items!.GroupBy(x => x.DishId).Any(group => group.Count() > 1))
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "Cart cannot contain duplicate dishes in the same meal.");
            }
        }

        var mealIds = data.Meals.Select(x => x.MealId).ToList();
        var meals = await mealRepository
            .GetQueryable(x => mealIds.Contains(x.Id))
            .Include(x => x.DishMeals)
            .Include(x => x.MealTemplates)
                .ThenInclude(x => x.Settings)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (meals.Count != mealIds.Count)
        {
            return Result.Failure<ValidatedCart>(
                Error.NullValue,
                "One or more meals were not found.");
        }

        var dishIds = data.Meals
            .SelectMany(x => x.Items!)
            .Select(x => x.DishId)
            .Distinct()
            .ToList();
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

        foreach (var mealData in data.Meals)
        {
            var meal = meals[mealData.MealId];
            var items = mealData.Items!;

            if (meal.IsDeleted || !meal.IsActive)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more meals are not available.");
            }

            if (DateTimeOffset.UtcNow > meal.AvailableForOrder)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more meals have passed the ordering deadline.");
            }

            var mealDishes = meal.DishMeals.ToDictionary(x => x.DishId);
            var mealDishIds = items.Select(x => x.DishId).ToList();
            if (mealDishIds.Any(dishId => !mealDishes.ContainsKey(dishId)))
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "One or more dishes do not belong to the selected meal.");
            }

            if (items.Any(item => item.Quantity > mealDishes[item.DishId].Quantity))
            {
                return Result.Failure<ValidatedCart>(
                    Error.InsufficientDishStock,
                    "One or more dishes do not have enough stock.");
            }

            var template = meal.MealTemplates.SingleOrDefault(x =>
                x.Id == mealData.MealTemplateId && !x.IsDeleted);
            if (template is null)
            {
                return Result.Failure<ValidatedCart>(
                    Error.InvalidValue,
                    "Meal template does not belong to the selected meal.");
            }

            var templateRuleResult = CartTemplateRuleValidator.Validate(
                template,
                items,
                dishes,
                requireCompleteTemplate);
            if (templateRuleResult.IsFailure)
            {
                return Result.Failure<ValidatedCart>(
                    templateRuleResult.Error!,
                    templateRuleResult.Message);
            }
        }

        return Result.Success(
            new ValidatedCart
            {
                Meals = meals,
                Dishes = dishes
            },
            "Cart data is valid.");
    }

    private static Result ValidateBasicMealData(CartMealData mealData)
    {
        if (mealData.MealId == Guid.Empty)
        {
            return Result.Failure(
                Error.InvalidValue,
                "MealId is required.");
        }

        if (mealData.MealTemplateId == Guid.Empty)
        {
            return Result.Failure(
                Error.InvalidValue,
                "MealTemplateId is required.");
        }

        if (mealData.Items is null || mealData.Items.Count == 0)
        {
            return Result.Failure(
                Error.InvalidValue,
                "Cart meal must contain at least one item.");
        }

        if (mealData.Items.Any(x => x.DishId == Guid.Empty))
        {
            return Result.Failure(
                Error.InvalidValue,
                "DishId is required for every cart item.");
        }

        if (mealData.Items.Any(x => x.Quantity <= 0))
        {
            return Result.Failure(
                Error.InvalidValue,
                "Item quantity must be greater than zero.");
        }

        return Result.Success("Cart meal data is valid.");
    }
}
