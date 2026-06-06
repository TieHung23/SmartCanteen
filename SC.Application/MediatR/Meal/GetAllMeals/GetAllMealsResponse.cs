namespace SC.Application.MediatR.Meal.GetAllMeals;

public class GetAllMealsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<DishMealDto> Dishes { get; set; } = new();
}

public class DishMealDto
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
