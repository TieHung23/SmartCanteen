using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Refund.Entity;
using SC.Domain.Domain.Refund.Enum;

namespace SC.Domain.Domain.Refund.AggregateRoot;

public class RefundRequest : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private RefundRequest()
    {
    }

    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyNameSnapshot { get; set; } = string.Empty;
    public decimal RefundPercentSnapshot { get; set; }
    public decimal OrderAmountSnapshot { get; set; }
    public decimal RefundAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public RefundRequestStatus Status { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public IList<RefundRequestImage> Images { get; set; } = new List<RefundRequestImage>();

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static RefundRequest Submit(
        Guid orderId,
        Guid userId,
        string policyCode,
        string policyName,
        decimal refundPercent,
        decimal orderAmount,
        string description)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(policyCode))
        {
            throw new ArgumentException("Policy code is required.", nameof(policyCode));
        }

        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name is required.", nameof(policyName));
        }

        if (refundPercent <= 0 || refundPercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(refundPercent),
                "Refund percent must be greater than zero and at most 100.");
        }

        if (orderAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderAmount),
                "Order amount must be greater than zero.");
        }

        var now = DateTimeOffset.UtcNow;
        var refundAmount = decimal.Round(
            orderAmount * refundPercent / 100,
            2,
            MidpointRounding.AwayFromZero);

        return new RefundRequest
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            PolicyCode = policyCode.Trim(),
            PolicyNameSnapshot = policyName.Trim(),
            RefundPercentSnapshot = refundPercent,
            OrderAmountSnapshot = orderAmount,
            RefundAmount = refundAmount,
            Description = description?.Trim() ?? string.Empty,
            Status = RefundRequestStatus.Pending,
            CreatedAtUtc = now,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }

    public void AddImage(RefundRequestImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.RefundRequestId = Id;
        Images.Add(image);
    }

    public void Approve(
        Guid reviewerId,
        Guid walletTransactionId)
    {
        EnsurePending();

        Status = RefundRequestStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        WalletTransactionId = walletTransactionId;
        UpdatedAtUtc = ReviewedAtUtc;
        UpdatedBy = reviewerId;
    }

    public void Reject(Guid reviewerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        }

        EnsurePending();

        Status = RefundRequestStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = reason.Trim();
        UpdatedAtUtc = ReviewedAtUtc;
        UpdatedBy = reviewerId;
    }

    private void EnsurePending()
    {
        if (Status != RefundRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Refund request is already {Status} and cannot be modified.");
        }
    }
}
