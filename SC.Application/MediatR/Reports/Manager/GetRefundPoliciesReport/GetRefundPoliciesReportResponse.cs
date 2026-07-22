namespace SC.Application.MediatR.Reports.Manager.GetRefundPoliciesReport;

public sealed class GetRefundPoliciesReportResponse
{
    public ReportRangeDto Range { get; set; } = new();
    public IReadOnlyList<RefundPolicyReportItem> Items { get; set; } = [];
}

public sealed class RefundPolicyReportItem
{
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int PendingRequests { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal ApprovalRate { get; set; }
}
