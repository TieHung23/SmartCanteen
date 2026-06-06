using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.Order.ValueObject;

public class OrderItem : Abstraction.Aggregates.ValueObject
{
    private OrderItem()
    {
    }

    public Guid DishId { get; set; }
    public Dish.AggregateRoot.Dish Dish { get; set; } = null!;

    public int Quantity { get; init; }
    public Money UnitPrice { get; init; } = Money.Create(0);

    public Guid OrderId { get; set; }

    public static OrderItem Create(Guid dishId, int quantity, decimal unitPriceAmount)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new OrderItem
        {
            DishId = dishId,
            Quantity = quantity,
            UnitPrice = Money.Create(unitPriceAmount)
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DishId;
        yield return Quantity;
        yield return UnitPrice;
        yield return OrderId;
    }
}