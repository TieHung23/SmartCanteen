using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.Order.ValueObject;

public class OrderItem : Abstraction.Aggregates.ValueObject
{
    private OrderItem() { }

    public Guid DishId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Create(0);

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
    }
}
