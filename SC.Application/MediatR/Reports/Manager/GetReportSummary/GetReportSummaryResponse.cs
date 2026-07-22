namespace SC.Application.MediatR.Reports.Manager.GetReportSummary;

public sealed class GetReportSummaryResponse
{
    public ReportRangeResponse Range { get; set; } = new();
    public ReportDashboardResponse Dashboard { get; set; } = new();
    public IReadOnlyList<RevenueTrendResponse> RevenueTrend { get; set; } = [];
    public IReadOnlyList<PopularDishResponse> PopularDishes { get; set; } = [];
    public IReadOnlyList<OrderStatResponse> OrderStats { get; set; } = [];
    public RefundStatsResponse RefundStats { get; set; } = new();
}

public sealed class ReportRangeResponse
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
}

public sealed class ReportDashboardResponse
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RefundRate { get; set; }
    public string TopDish { get; set; } = string.Empty;
    public int NewCustomers { get; set; }
    public int TotalComplaints { get; set; }
    public int ActiveSessions { get; set; }
    public decimal OrderChange { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal RefundChange { get; set; }
    public decimal CustomerChange { get; set; }
}

public sealed class RevenueTrendResponse
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
}

public sealed class PopularDishResponse
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int TotalQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class OrderStatResponse
{
    public int Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class RefundStatsResponse
{
    public int TotalRefunds { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public int ApprovedRefunds { get; set; }
    public int RejectedRefunds { get; set; }
    public int PendingRefunds { get; set; }
    public decimal RefundRate { get; set; }
    public IReadOnlyList<RefundBreakdownResponse> Breakdown { get; set; } = [];
}

public sealed class RefundBreakdownResponse
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal Rate { get; set; }
}
