namespace SC.Contract.Services.Notification;

/// <summary>Payload real-time khi đơn đổi trạng thái (FE cập nhật badge live).</summary>
public sealed record OrderStatusRealtimeEvent(Guid OrderId, int Status, string StatusName);

/// <summary>
/// Đẩy sự kiện đổi trạng thái đơn xuống 1 user qua SignalR (event "OrderStatusChanged").
/// Hiện thực ở tầng Api trên NotificationHub. BEST-EFFORT.
/// </summary>
public interface IOrderStatusRealtimePublisher
{
    Task PublishAsync(
        Guid recipientUserId,
        OrderStatusRealtimeEvent evt,
        CancellationToken cancellationToken = default);
}
