namespace SC.Contract.Services.Visualization;

/// <summary>
/// Một sự kiện "digital twin" BE đẩy xuống Unity (group "visualization", event "ServingEvent").
/// Một record phẳng, phân loại bằng <see cref="Type"/> để Unity chỉ cần 1 lớp deserialize.
/// Field thừa để null — mỗi loại event chỉ dùng phần liên quan.
/// </summary>
/// <param name="Type">
/// Loại: jobCreated | jobReceived | trayScanned | pickStarted | pickCompleted |
/// dishVerified | placeCompleted | onShelf | servingFailed |
/// pickupAssigned | collected | expired | shelfRefilled.
/// </param>
/// <param name="OrderId">Đơn liên quan (khoá gom event trên Unity).</param>
/// <param name="JobId">ServingJob (nếu có).</param>
/// <param name="Station">Trạm/tay báo việc, ví dụ "S1".</param>
/// <param name="Lane">Lane gắp, ví dụ "S1_L2".</param>
/// <param name="DishId">Món đang thao tác (nếu event ở mức món).</param>
/// <param name="DishName">Tên món — BE enrich sẵn để Unity hiển thị, khỏi tra thêm.</param>
/// <param name="ItemIndex">Thứ tự món trong đơn (0-based) — Unity xếp animation.</param>
/// <param name="ItemCount">Tổng số món của đơn.</param>
/// <param name="Quantity">Số lượng (event shelfRefilled: số hộp staff vừa nạp thêm).</param>
/// <param name="TrayCode">Mã khay mẹ (event trayScanned / pickupAssigned).</param>
/// <param name="PickupSlotCode">Ô nhận hàng (event pickupAssigned / collected / expired).</param>
/// <param name="Message">Thông điệp hiển thị / lý do lỗi.</param>
/// <param name="TimestampUtc">Mốc thời gian; để default thì visualizer tự đóng dấu UtcNow.</param>
public sealed record ServingVisualEvent(
    string Type,
    Guid OrderId,
    Guid? JobId = null,
    string? Station = null,
    string? Lane = null,
    Guid? DishId = null,
    string? DishName = null,
    int? ItemIndex = null,
    int? ItemCount = null,
    int? Quantity = null,
    string? TrayCode = null,
    string? PickupSlotCode = null,
    string? Message = null,
    DateTime TimestampUtc = default);
