namespace SC.Domain.Domain.Order.Enum;

public enum OrderStatus
{
    Pending = 0,
    ReadyForPickup = 1,
    Completed = 2,
    Cancelled = 3,

    // Mở rộng cho luồng robot. GIỮ NGUYÊN giá trị int cũ (đừng renumber — DB lưu int).
    Preparing = 4,      // đã thanh toán, robot đang ráp khay
    Expired = 7         // no-show quá hạn lấy -> staff dọn, ô về Empty
    // (đã bỏ Serving=5 [trùng vai ServingJob], InHoldingArea=6 & Disposed=8 [gom vào Expired])
}