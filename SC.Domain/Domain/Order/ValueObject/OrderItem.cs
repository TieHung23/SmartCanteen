using SC.Domain.Domain.User.ValueObject;
using DishesAggregate = SC.Domain.Domain.Dishes.AggregateRoot.Dishes;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Domain.Domain.Order.ValueObject;

public class OrderItem : Abstraction.Aggregates.ValueObject
{
    private OrderItem()
    {
    }

    public Guid DishesId { get; set; }
    public DishesAggregate? Dishes { get; set; }

    public int Quantity { get; init; }
    public Money UnitPrice { get; init; } = Money.Create(0);

    public Guid OrderId { get; set; }
    public OrderAggregate? Order { get; set; }

    public static OrderItem Create(DishesAggregate dishes, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new OrderItem
        {
            Dishes = dishes,
            DishesId = dishes.Id,
            Quantity = quantity,
            UnitPrice = dishes.Price
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DishesId;
        yield return Quantity;
        yield return UnitPrice;
        yield return OrderId;
    }
}