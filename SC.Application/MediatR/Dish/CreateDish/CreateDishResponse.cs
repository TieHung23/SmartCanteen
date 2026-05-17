namespace SC.Application.MediatR.Dish.CreateDish;

public class CreateDishResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public Guid MealId { get; set; }
    public Guid CategoryId { get; set; }
}
