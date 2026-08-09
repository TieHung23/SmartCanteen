using Microsoft.AspNetCore.SignalR;
using SC.Api.Hubs;
using SC.Contract.Services.Notification;

namespace SC.Api.Services;

/// <summary>
/// Đẩy sự kiện đổi trạng thái đơn xuống FE/mobile qua NotificationHub (kênh FE đã kết nối sẵn),
/// event "OrderStatusChanged". FE nghe event này -> cập nhật badge status của đơn đang xem.
/// </summary>
public sealed class SignalROrderStatusRealtimePublisher(IHubContext<NotificationHub> hubContext)
    : IOrderStatusRealtimePublisher
{
    public const string EventName = "OrderStatusChanged";

    public Task PublishAsync(
        Guid recipientUserId,
        OrderStatusRealtimeEvent evt,
        CancellationToken cancellationToken = default)
        => hubContext.Clients
            .User(recipientUserId.ToString())
            .SendAsync(EventName, evt, cancellationToken);
}
