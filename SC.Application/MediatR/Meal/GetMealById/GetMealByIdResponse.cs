namespace SC.Application.MediatR.Meal.GetMealById;

public class GetMealByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealTemplateDto> MealTemplates { get; set; } = new();
    public List<DishMealDto> Dishes { get; set; } = new();
}

public class MealTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public List<MealSettingDto> Settings { get; set; } = new();
}

public class MealSettingDto
{
    public Guid CategoryId { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
    public bool IsRequired { get; set; }
}

public class DishMealDto
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
