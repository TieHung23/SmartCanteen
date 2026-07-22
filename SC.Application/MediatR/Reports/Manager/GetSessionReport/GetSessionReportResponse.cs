namespace SC.Application.MediatR.Reports.Manager.GetSessionReport;

public sealed class GetSessionReportResponse
{
    public ReportRangeDto Range { get; set; } = new();
    public IReadOnlyList<SessionReportItem> Items { get; set; } = [];
}

public sealed class SessionReportItem
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTimeOffset AvailableFrom { get; set; }
    public DateTimeOffset AvailableTo { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ExpiredOrders { get; set; }
    public decimal Revenue { get; set; }
    public int RefundRequests { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal RefundRate { get; set; }
}
