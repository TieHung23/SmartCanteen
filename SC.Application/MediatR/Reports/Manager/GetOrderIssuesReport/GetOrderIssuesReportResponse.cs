namespace SC.Application.MediatR.Reports.Manager.GetOrderIssuesReport;

public sealed class GetOrderIssuesReportResponse
{
    public ReportRangeDto Range { get; set; } = new();
    public int TotalOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ExpiredOrders { get; set; }
    public int RefundRequestedOrders { get; set; }
    public decimal CancelledRate { get; set; }
    public decimal ExpiredRate { get; set; }
    public decimal RefundRequestRate { get; set; }
    public IReadOnlyList<OrderIssueReportItem> Items { get; set; } = [];
}

public sealed class OrderIssueReportItem
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Rate { get; set; }
}
