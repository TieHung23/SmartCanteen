using SC.Contract.Shared;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using MealTemplateEntity = SC.Domain.Domain.Session.Entity.MealTemplate;

namespace SC.Application.MediatR.Cart;

public static class CartTemplateRuleValidator
{
    public static Result Validate(
        MealTemplateEntity template,
        IReadOnlyCollection<CartItemData> items,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishes,
        bool requireCompleteTemplate)
    {
        var activeSettings = template.Settings
            .Where(x => !x.IsDeleted)
            .ToList();

        if (activeSettings.GroupBy(x => x.CategoryId).Any(group => group.Count() > 1))
        {
            return Result.Failure(
                Error.InvalidValue,
                "Meal template contains duplicate category settings.");
        }

        var settingsByCategory = activeSettings.ToDictionary(x => x.CategoryId);

        var quantitiesByCategory = items
            .GroupBy(item => dishes[item.DishId].CategoryId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Quantity));

        if (quantitiesByCategory.Keys.Any(categoryId => !settingsByCategory.ContainsKey(categoryId)))
        {
            return Result.Failure(
                Error.InvalidValue,
                "One or more selected dish categories are not allowed by the meal template.");
        }

        foreach (var setting in settingsByCategory.Values)
        {
            quantitiesByCategory.TryGetValue(setting.CategoryId, out var selectedQuantity);

            if (requireCompleteTemplate && setting.IsRequired && selectedQuantity < setting.MinQuantity)
            {
                return Result.Failure(
                    Error.InvalidValue,
                    "The cart does not satisfy the required category quantities.");
            }

            if (requireCompleteTemplate && selectedQuantity > 0 && selectedQuantity < setting.MinQuantity)
            {
                return Result.Failure(
                    Error.InvalidValue,
                    "A selected category does not meet its minimum quantity.");
            }

            if (selectedQuantity > setting.MaxQuantity)
            {
                return Result.Failure(
                    Error.InvalidValue,
                    "A selected category exceeds its maximum quantity.");
            }
        }

        return Result.Success("Meal template rules are satisfied.");
    }
}
