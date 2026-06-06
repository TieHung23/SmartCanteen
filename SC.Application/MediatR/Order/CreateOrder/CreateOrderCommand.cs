using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.CreateOrder;

public class CreateOrderCommand : ICommand<CreateOrderResponse>
{
    public Guid MealId { get; set; }
    public List<OrderItemInput> Items { get; set; } = new();
}

public class OrderItemInput
{
    public Guid DishId { get; set; }
    public int Quantity { get; set; }
}
