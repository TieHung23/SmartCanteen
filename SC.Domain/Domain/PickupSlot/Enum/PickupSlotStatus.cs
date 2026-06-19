namespace SC.Domain.Domain.PickupSlot.Enum;

/// <summary>
/// Trạng thái ô kệ pickup (nơi HS quét QR tới lấy). Khác SlotConfiguration (lane robot gắp).
/// </summary>
public enum PickupSlotStatus
{
    Empty = 0,           // trống, sẵn sàng nhận khay
    Occupied = 1,        // có khay (order) đang chờ
    WaitingCollect = 2   // HS đã xác thực QR, đang chờ rút khay ra
}
