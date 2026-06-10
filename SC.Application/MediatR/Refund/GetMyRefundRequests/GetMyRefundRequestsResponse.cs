namespace SC.Application.MediatR.Refund.GetMyRefundRequests;

public sealed class GetMyRefundRequestsResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public decimal RefundPercent { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ImageCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
}
