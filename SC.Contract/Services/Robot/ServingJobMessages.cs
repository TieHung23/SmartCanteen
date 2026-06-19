namespace SC.Contract.Services.Robot;

/// <summary>1 món trong job phục vụ (BE -&gt; robot service).</summary>
public sealed class ServingJobItemMessage
{
    public Guid DishId { get; init; }
    public string? DishName { get; init; }
    public int Quantity { get; init; }
    public string? Station { get; init; }   // trạm/lane robot gắp (nếu đã cấu hình)
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
    public string? Message { get; init; }
}
