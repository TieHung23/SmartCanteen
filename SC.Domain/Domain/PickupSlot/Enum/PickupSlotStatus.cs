namespace SC.Domain.Domain.PickupSlot.Enum;

/// <summary>
/// Trạng thái ô kệ pickup (nơi HS quét QR tới lấy). Khác SlotConfiguration (lane robot gắp).
/// </summary>
public enum PickupSlotStatus
{
    Empty = 0,      // trống, sẵn sàng nhận đồ
    Occupied = 1    // có đồ (order) đang chờ HS -> HS quét QR = Empty (không sensor)
    // (đã bỏ WaitingCollect=2: không có sensor nên không biết lúc lấy xong; quét = xong luôn)
}
