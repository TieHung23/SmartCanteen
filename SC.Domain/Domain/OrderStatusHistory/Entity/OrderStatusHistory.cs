using SC.Domain.Abstraction.Entities;
using OrderStatusEnum = SC.Domain.Domain.Order.Enum.OrderStatus;

namespace SC.Domain.Domain.OrderStatusHistory.Entity;

public class OrderStatusHistory : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private OrderStatusHistory() { }

    public Guid OrderId { get; private set; }
    public OrderStatusEnum? FromStatus { get; private set; }
    public OrderStatusEnum ToStatus { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Note { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static OrderStatusHistory Create(Guid orderId, OrderStatusEnum? fromStatus, OrderStatusEnum toStatus, Guid changedBy, string? reasonCode = null, string? note = null)
    {
        return new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = orderId, FromStatus = fromStatus, ToStatus = toStatus, ReasonCode = reasonCode, Note = note, CreatedAtUtc = DateTimeOffset.UtcNow, CreatedBy = changedBy, UpdatedBy = changedBy };
    }

    public void SoftDelete() { IsDeleted = true; DeletedAtUtc = DateTimeOffset.UtcNow; }
}
