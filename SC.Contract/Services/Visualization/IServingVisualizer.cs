namespace SC.Contract.Services.Visualization;

/// <summary>
/// Đẩy sự kiện digital-twin xuống Unity. Hiện thực bằng SignalR ở tầng Api.
/// BEST-EFFORT: mọi lỗi đẩy event được nuốt bên trong hiện thực — KHÔNG bao giờ
/// làm hỏng luồng nghiệp vụ nếu Unity/hub gặp sự cố.
/// </summary>
public interface IServingVisualizer
{
    Task PublishAsync(ServingVisualEvent evt, CancellationToken cancellationToken = default);
}
