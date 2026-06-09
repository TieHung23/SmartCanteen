using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequestDetail;

public sealed record GetRefundRequestDetailQuery(Guid Id)
    : IQuery<GetRefundRequestDetailResponse>;

public sealed class GetRefundRequestDetailResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public decimal RefundPercent { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<ManagerRefundImageResponse> Images { get; set; } = [];
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ManagerRefundImageResponse
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
