using SC.Domain.Abstraction.Entities;
using OrderStatusEnum = SC.Domain.Domain.Order.Enum.OrderStatus;

namespace SC.Domain.Domain.OrderStatusHistory.Entity;

/// <summary>
/// Nhật ký chuyển trạng thái đơn (audit trail). Mỗi lần Order đổi Status -&gt; 1 dòng.
/// </summary>
public class OrderStatusHistory : Entity<Guid>, IAuditableEntity<Guid>
{
    private OrderStatusHistory()
    {
    }

    public required Guid OrderId { get; set; }
    public OrderStatusEnum? FromStatus { get; set; }
    public required OrderStatusEnum ToStatus { get; set; }
    public string? ReasonCode { get; set; }
    public string? Note { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static OrderStatusHistory Create(
        Guid orderId,
        OrderStatusEnum? fromStatus,
        OrderStatusEnum toStatus,
        Guid changedBy,
        string? reasonCode = null,
        string? note = null)
    {
        return new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ReasonCode = reasonCode,
            Note = note,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = changedBy,
            UpdatedBy = changedBy
        };
    }
}
