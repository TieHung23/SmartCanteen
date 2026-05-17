using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Dish.UpdateDishStock;

public class UpdateDishStockCommand : ICommand<UpdateDishStockResponse>
{
    public Guid Id { get; set; }
    public int StockQuantity { get; set; }
}
