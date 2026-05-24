namespace SC.Application.MediatR.Meal.GetMealById;

public class GetMealByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealSettingDto> MealSettings { get; set; } = new();
}

public class MealSettingDto
{
    public Guid CategoryId { get; set; }
    public int Quantity { get; set; }
}
