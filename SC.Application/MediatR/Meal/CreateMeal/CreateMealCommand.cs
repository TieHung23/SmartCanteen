using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Meal.CreateMeal;

public class CreateMealCommand : ICommand<CreateMealResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "VND";
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealSettingInput> MealSettings { get; set; } = new();
}

public class MealSettingInput
{
    public Guid CategoryId { get; set; }
    public int Quantity { get; set; }
}
