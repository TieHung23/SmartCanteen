using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.Order.Entity;

public class OrderItem : Entity<int>
{
    private OrderItem() { }

    public Guid DishId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Create(0);
    public OrderItemStatus ItemStatus { get; private set; } = OrderItemStatus.Pending;

    public static OrderItem Create(Guid dishId, int quantity, decimal unitPriceAmount)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new OrderItem
        {
            Id = 0,
            DishId = dishId,
            Quantity = quantity,
            UnitPrice = Money.Create(unitPriceAmount),
            ItemStatus = OrderItemStatus.Pending
        };
    }

    public void Confirm()
    {
        if (ItemStatus != OrderItemStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.Confirmed;
    }

    public void MarkChangePending()
    {
        if (ItemStatus != OrderItemStatus.Pending)
            throw new InvalidOperationException($"Cannot mark change-pending item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.ChangePending;
    }

    public void SwapDish(Guid newDishId, decimal newUnitPriceAmount)
    {
        if (ItemStatus != OrderItemStatus.ChangePending)
            throw new InvalidOperationException($"Cannot swap dish in status {ItemStatus}.");
        DishId = newDishId;
        UnitPrice = Money.Create(newUnitPriceAmount);
        ItemStatus = OrderItemStatus.Swapped;
    }

    public void RefundItem()
    {
        if (ItemStatus != OrderItemStatus.ChangePending && ItemStatus != OrderItemStatus.Pending)
            throw new InvalidOperationException($"Cannot refund item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.Refunded;
    }

    public void MarkRefundPending()
    {
        if (ItemStatus != OrderItemStatus.ChangePending && ItemStatus != OrderItemStatus.Pending)
            throw new InvalidOperationException($"Cannot mark refund-pending item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.RefundPending;
    }

    public void CompleteRefund()
    {
        if (ItemStatus != OrderItemStatus.RefundPending)
            throw new InvalidOperationException($"Cannot complete refund item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.Refunded;
    }

    public void CancelRefund()
    {
        if (ItemStatus != OrderItemStatus.RefundPending)
            throw new InvalidOperationException($"Cannot cancel refund item in status {ItemStatus}.");
        ItemStatus = OrderItemStatus.ChangePending;
    }
}
