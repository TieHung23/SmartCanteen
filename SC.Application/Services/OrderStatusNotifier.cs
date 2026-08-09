using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.User.Enum;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.Services;

internal sealed class OrderStatusNotifier(
    IGenericRepository<UserAggregate, Guid> userRepository,
    IOrderStatusRealtimePublisher realtimePublisher,
    ILogger<OrderStatusNotifier> logger) : IOrderStatusNotifier
{
    public async Task BroadcastAsync(
        Guid orderId,
        Guid studentUserId,
        int status,
        string statusName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var evt = new OrderStatusRealtimeEvent(orderId, status, statusName);

            // Người nhận: Học Sinh chủ đơn + TOÀN BỘ Staff (dashboard staff cũng cập nhật live).
            var recipients = new HashSet<Guid> { studentUserId };
            var staff = await userRepository.FindListAsync(
                u => !u.IsDeleted && u.Role == Role.Staff, cancellationToken);
            foreach (var s in staff) recipients.Add(s.Id);

            foreach (var uid in recipients)
                await realtimePublisher.PublishAsync(uid, evt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast order status for order {OrderId}", orderId);
        }
    }
}
