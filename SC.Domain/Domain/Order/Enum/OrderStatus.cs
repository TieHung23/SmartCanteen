namespace SC.Domain.Domain.Order.Enum;

public enum OrderStatus
{
    Pending = 0,
    ReadyForPickup = 1,
    Completed = 2,
    Cancelled = 3,

    // Mở rộng cho luồng robot (PUSH/BUFFER). Append-only, không đổi giá trị cũ.
    Preparing = 4,      // đã thanh toán, robot đang ráp khay
    Serving = 5,        // robot đang gắp/đặt món
    InHoldingArea = 6,  // quá hạn lấy -> chuyển khu giữ thủ công (staff)
    Expired = 7,        // hết hạn lấy
    Disposed = 8        // đã huỷ bỏ phần ăn ở khu giữ
}