using SC.Domain.Domain.User.ValueObject;
using ProductAggregate = SC.Domain.Domain.Product.AggregateRoot.Product;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Domain.Domain.Order.ValueObject;

public class OrderItem : Abstraction.Aggregates.ValueObject
{
    public Guid ProductId { get; set; }
    public ProductAggregate? Product { get; set; }

    public int Quantity { get; init; }
    public Money UnitPrice { get; init; } = Money.Create(0);

    public Guid OrderId { get; set; }
    public OrderAggregate? Order { get; set; }

    private OrderItem() { }

    public static OrderItem Create(ProductAggregate product, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new OrderItem
        {
            Product = product,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = product.Price
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return Quantity;
        yield return UnitPrice;
        yield return OrderId;
    }
}