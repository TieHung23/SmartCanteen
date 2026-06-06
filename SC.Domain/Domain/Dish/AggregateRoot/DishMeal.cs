using MealAggregateRoot = SC.Domain.Domain.Meal.AggregateRoot.Meal;

namespace SC.Domain.Domain.Dish.AggregateRoot;

public class DishMeal
{
    public Guid DishId { get; set; }
    public Guid MealId { get; set; }
    public int Quantity { get; set; }

    public Dish Dish { get; set; } = null!;
    public MealAggregateRoot Meal { get; set; } = null!;
}
