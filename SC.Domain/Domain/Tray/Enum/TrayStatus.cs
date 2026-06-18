namespace SC.Domain.Domain.Tray.Enum;

/// <summary>
/// Trạng thái khay tái dùng (pool). Mỗi khay có mã (ArUco/barcode) để robot &amp; sensor nhận diện.
/// </summary>
public enum TrayStatus
{
    Available = 0,  // rảnh trong pool, có thể gán cho order mới
    Reserved = 1,   // đã gán cho 1 order, chờ robot ráp
    InUse = 2,      // robot đang ráp món lên khay
    AtSlot = 3      // khay đã nằm trên ô kệ pickup, chờ HS lấy
}
