namespace SC.Domain.Domain.ServingJob.Enum;

/// <summary>
/// Vòng đời job phục vụ (PUSH/BUFFER): BE bind Order↔Tray khi thanh toán xong,
/// đẩy (SignalR) cho robot service ráp sẵn lên kệ pickup.
/// </summary>
public enum ServingJobStatus
{
    Queued = 0,        // vừa tạo, chờ đẩy cho robot service
    Pushed = 1,        // đã đẩy (SignalR) cho robot service
    Assembling = 2,    // robot đang gắp/ráp khay
    OnShelf = 3,       // khay đã đặt lên kệ pickup, chờ HS lấy
    Collected = 4,     // HS đã lấy (quét QR pickup)
    Failed = 5,        // lỗi (robot/gripper) -> cần staff can thiệp
    Cancelled = 6      // order huỷ/refund -> không phục vụ
}
