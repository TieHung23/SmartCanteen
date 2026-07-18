namespace SC.Domain.Domain.RobotArm.Enum;

/// <summary>
/// Mã VỊ TRÍ lane trên kệ gravity: {trạm}_L{1..3}. 3 trạm × 3 lane (xem <see cref="LaneCatalog.LanesPerStation"/>).
/// Dùng làm kiểu field LaneCode trên command SlotConfiguration -> FE/Swagger có dropdown cố định.
/// Lưu xuống DB dạng chuỗi (ToString()); thêm trạm S4 sau này = thêm giá trị ở đây + tạo RobotArm.
/// </summary>
public enum LaneCode
{
    S1_L1,
    S1_L2,
    S1_L3,
    S2_L1,
    S2_L2,
    S2_L3,
    S3_L1,
    S3_L2,
    S3_L3
}
