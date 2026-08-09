namespace SC.Contract.Services.Notification;

/// <summary>
/// Bắn real-time sự kiện đổi trạng thái đơn cho <b>Học Sinh chủ đơn</b> + <b>toàn bộ Staff</b>
/// để FE/mobile cập nhật badge status live (không phải refresh tay). BEST-EFFORT — không làm hỏng
/// luồng nghiệp vụ nếu hub lỗi.
/// </summary>
public interface IOrderStatusNotifier
{
    Task BroadcastAsync(
        Guid orderId,
        Guid studentUserId,
        int status,
        string statusName,
        CancellationToken cancellationToken = default);
}
