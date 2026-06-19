using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Order.Entity;
using SC.Domain.Domain.Order.Enum;

namespace SC.Domain.Domain.Order.AggregateRoot;

public class Order : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private readonly List<OrderItem> _orderItems = [];

    private Order() { }

    public Guid SessionId { get; private set; }
    public Guid? MealTemplateId { get; private set; }
    public Guid? WalletTransactionId { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Order Create(Guid sessionId, Guid mealTemplateId, Guid createdBy)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            MealTemplateId = mealTemplateId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            Status = OrderStatus.Pending
        };
    }

    public void ChangeSession(Guid sessionId, Guid updatedBy)
    {
        SessionId = sessionId;
        Touch(updatedBy);
    }

    public void AddDish(Guid dishId, int quantity, decimal unitPriceAmount)
    {
        _orderItems.Add(OrderItem.Create(dishId, quantity, unitPriceAmount));
    }

    public void AttachTransaction(Guid walletTransactionId, Guid updatedBy)
    {
        WalletTransactionId = walletTransactionId;
        Touch(updatedBy);
    }

    public void RemoveDish(OrderItem orderItem)
    {
        _orderItems.Remove(orderItem);
    }

    public void UpdateStatus(OrderStatus status, Guid updatedBy)
    {
        Status = status;
        Touch(updatedBy);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
