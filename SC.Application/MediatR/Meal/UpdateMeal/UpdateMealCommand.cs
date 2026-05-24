using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Meal.UpdateMeal;

public class UpdateMealCommand : ICommand<UpdateMealResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "VND";
    public bool IsActive { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public List<MealSettingUpdate> MealSettings { get; set; } = new();
}

public class MealSettingUpdate
{
    public Guid CategoryId { get; set; }
    public int Quantity { get; set; }
}
