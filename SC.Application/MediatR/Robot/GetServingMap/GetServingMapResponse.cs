namespace SC.Application.MediatR.Robot.GetServingMap;

/// <summary>1 lane trong bản đồ kệ: mã lane (vd "S1_L1") -&gt; món kỳ vọng (dishName, đúng như DB).</summary>
public sealed record ServingMapLane(string LaneCode, string? DishName);

/// <summary>Bản đồ kệ của (các) ca đang mở. Rỗng nếu không có ca nào mở / chưa cấu hình lane.</summary>
public sealed record GetServingMapResponse(IReadOnlyList<ServingMapLane> Lanes);
