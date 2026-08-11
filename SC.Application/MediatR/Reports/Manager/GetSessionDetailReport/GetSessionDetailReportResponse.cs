namespace SC.Application.MediatR.Reports.Manager.GetSessionDetailReport;

public sealed class GetSessionDetailReportResponse
{
    public SessionDetailInfoResponse Session { get; set; } = new();
    public SessionDetailSummaryResponse Summary { get; set; } = new();
    public IReadOnlyList<SessionTimelineItemResponse> Timeline { get; set; } = [];
    public IReadOnlyList<SessionOrderTrendResponse> OrderTrend { get; set; } = [];
    public IReadOnlyList<SessionOrderStatResponse> OrderStats { get; set; } = [];
    public IReadOnlyList<SessionPopularDishResponse> PopularDishes { get; set; } = [];
    public IReadOnlyList<SessionRecentOrderResponse> RecentOrders { get; set; } = [];
}

public sealed class SessionDetailInfoResponse
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsFinalized { get; set; }
    public DateTimeOffset AvailableForOrder { get; set; }
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public DateTimeOffset? FinalizationDeadline { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
}

public sealed class SessionDetailSummaryResponse
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int PreparingOrders { get; set; }
    public int ReadyForPickupOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ExpiredOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int RefundRequests { get; set; }
    public int ApprovedRefunds { get; set; }
    public int RejectedRefunds { get; set; }
    public int PendingRefunds { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancelRate { get; set; }
    public decimal RefundRate { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public sealed class SessionTimelineItemResponse
{
    public DateTimeOffset Time { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class SessionOrderTrendResponse
{
    public DateTimeOffset TimeBucket { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class SessionOrderStatResponse
{
    public int Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class SessionPopularDishResponse
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }
    public int TotalOrders { get; set; }
    public int TotalQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class SessionRecentOrderResponse
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int Status { get; set; }
    public decimal TotalPrice { get; set; }
    public int ItemCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
