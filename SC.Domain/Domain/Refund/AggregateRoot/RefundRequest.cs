using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Refund.Entity;
using SC.Domain.Domain.Refund.Enum;

namespace SC.Domain.Domain.Refund.AggregateRoot;

public class RefundRequest : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private readonly List<RefundRequestImage> _images = [];

    private RefundRequest() { }

    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public int? OrderItemId { get; private set; }
    public Guid? ChangeProposalId { get; private set; }
    public Guid? DishId { get; private set; }
    public string PolicyCode { get; private set; } = string.Empty;
    public string PolicyNameSnapshot { get; private set; } = string.Empty;
    public decimal RefundPercentSnapshot { get; private set; }
    public decimal OrderAmountSnapshot { get; private set; }
    public decimal RefundAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public RefundRequestStatus Status { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? WalletTransactionId { get; private set; }
    public IReadOnlyCollection<RefundRequestImage> Images => _images.AsReadOnly();
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

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
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(policyCode))
            throw new ArgumentException("Policy code is required.", nameof(policyCode));
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentException("Policy name is required.", nameof(policyName));
        if (refundPercent <= 0 || refundPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(refundPercent), "Refund percent must be greater than zero and at most 100.");
        if (orderAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(orderAmount), "Order amount must be greater than zero.");

        var now = DateTimeOffset.UtcNow;
        var refundAmount = decimal.Round(orderAmount * refundPercent / 100, 2, MidpointRounding.AwayFromZero);

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

    public void AttachProposalContext(
        int? orderItemId,
        Guid? changeProposalId,
        Guid? dishId)
    {
        if (orderItemId.HasValue && orderItemId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(orderItemId), "Order item ID must be greater than zero.");
        if (changeProposalId.HasValue && changeProposalId.Value == Guid.Empty)
            throw new ArgumentException("Change proposal ID cannot be empty.", nameof(changeProposalId));
        if (dishId.HasValue && dishId.Value == Guid.Empty)
            throw new ArgumentException("Dish ID cannot be empty.", nameof(dishId));

        OrderItemId = orderItemId;
        ChangeProposalId = changeProposalId;
        DishId = dishId;
    }

    public void AddImage(RefundRequestImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _images.Add(image);
    }

    public void Approve(Guid reviewerId, Guid walletTransactionId)
    {
        EnsurePending();
        Status = RefundRequestStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        WalletTransactionId = walletTransactionId;
        Touch(reviewerId);
    }

    public void Reject(Guid reviewerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        EnsurePending();
        Status = RefundRequestStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = reason.Trim();
        Touch(reviewerId);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != RefundRequestStatus.Pending)
            throw new InvalidOperationException($"Refund request is already {Status} and cannot be modified.");
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
