namespace SC.Application.MediatR.Refund.GetRefundRequestById;

public sealed class GetRefundRequestByIdResponse
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
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<RefundImageResponse> Images { get; set; } = [];
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class RefundImageResponse
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
