namespace SC.Application.MediatR.Refund.SubmitRefundRequest;

public sealed class SubmitRefundRequestResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public int? OrderItemId { get; set; }
    public Guid? ChangeProposalId { get; set; }
    public Guid? DishId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public decimal RefundPercent { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
}
