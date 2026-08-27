namespace SC.Contract.Services.Robot;

/// <summary>
/// 1 món trong job phục vụ (BE -&gt; robot service). Nhãn Station/LaneCode lấy từ
/// SlotConfiguration của ca; món chưa cấu hình -&gt; cả hai null (edge/staff xử lý).
/// </summary>
public sealed class ServingJobItemMessage
{
    public Guid DishId { get; init; }
    public string? DishName { get; init; }
    public int Quantity { get; init; }
    public string? Station { get; init; }    // TAY nào gắp: mã RobotArm, vd "S1"
    public string? LaneCode { get; init; }   // GẮP Ở ĐÂU: mã lane, vd "S1_L2" (UNDERSCORE; edge tra teaching point)
    public bool Done { get; init; }          // đã phục vụ ĐỦ số lượng lượt trước (PlacedCount>=Quantity) -> edge SKIP khi requeue
    public int PlacedCount { get; init; }    // #10: SỐ TÔ đã đặt (đếm log PlaceCompleted) -> requeue chỉ đặt (Quantity - PlacedCount) tô còn thiếu
}

/// <summary>Job phục vụ đẩy cho robot service qua SignalR (event "ReceiveJob").</summary>
public sealed class ServingJobMessage
{
    public Guid JobId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? TrayId { get; init; }
    public string? TrayCode { get; init; }
    public IReadOnlyList<ServingJobItemMessage> Items { get; init; } = [];
}

/// <summary>Robot service báo trạng thái ngược về BE (hub method "ReportStatus").</summary>
public sealed class ServingStatusUpdate
{
    public Guid OrderId { get; init; }
    public Guid? TrayId { get; init; }
    public string State { get; init; } = string.Empty;   // Assembling/PickStarted/PickCompleted/PlaceCompleted/Failed...
    public string? Station { get; init; }
    public Guid? DishId { get; init; }                   // món của event mức món (PickCompleted/Error); null cho event mức job
    public string? Message { get; init; }
}
