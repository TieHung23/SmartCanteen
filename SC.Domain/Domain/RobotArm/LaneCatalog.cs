using System.Text.RegularExpressions;

namespace SC.Domain.Domain.RobotArm;

/// <summary>
/// Nguồn chân lý danh sách LANE hợp lệ theo trạm. Mỗi trạm (RobotArm.Code, vd "S1")
/// có <see cref="LanesPerStation"/> lane vật lý: {Code}_L1 .. {Code}_L3.
/// Lane = VỊ TRÍ trên kệ (WHERE robot gắp), độc lập với chuỗi động tác (HOW) ở Edge.
/// Đổi số lane/trạm chỉ sửa 1 hằng số ở đây; thêm trạm = thêm RobotArm (data), không cần sửa code.
/// </summary>
public static class LaneCatalog
{
    public const int LanesPerStation = 3;

    /// <summary>Lane của 1 trạm: "S1" -> ["S1_L1","S1_L2","S1_L3"].</summary>
    public static IReadOnlyList<string> ForStation(string armCode) =>
        Enumerable.Range(1, LanesPerStation)
            .Select(i => $"{armCode}_L{i}")
            .ToList();

    /// <summary>
    /// Hợp lệ khi: có trạm -> lane phải thuộc đúng trạm đó; không trạm -> chỉ cần đúng dạng {tên}_L[1-3].
    /// So sánh không phân biệt hoa thường.
    /// </summary>
    public static bool IsValidLane(string laneCode, string? armCode)
    {
        if (!string.IsNullOrWhiteSpace(armCode))
            return ForStation(armCode)
                .Any(x => string.Equals(x, laneCode, StringComparison.OrdinalIgnoreCase));

        return Regex.IsMatch(laneCode, @"^.+_L[1-3]$", RegexOptions.IgnoreCase);
    }
}
