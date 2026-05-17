namespace SC.Application.MediatR.Dish.UpdateDishStock;

public class UpdateDishStockResponse
{
    public Guid Id { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}
