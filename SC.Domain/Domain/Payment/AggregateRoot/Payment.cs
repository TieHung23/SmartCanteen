using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Payment.Enum;

namespace SC.Domain.Domain.Payment.AggregateRoot;

public class Payment : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private Payment() { }

    public string GatewayOrderId { get; private set; } = string.Empty;
    public string? GatewayTransactionId { get; private set; }
    public decimal AmountVnd { get; private set; }
    public decimal ConvertedPoints { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentType Type { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static Payment Create(
        string gatewayOrderId,
        decimal amountVnd,
        decimal convertedPoints,
        PaymentMethod method,
        Guid userId,
        Guid createdBy,
        PaymentType type)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            GatewayOrderId = gatewayOrderId,
            AmountVnd = amountVnd,
            ConvertedPoints = convertedPoints,
            Status = PaymentStatus.Pending,
            Method = method,
            UserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            Type = type
        };
    }

    public void MarkAsCompleted(string? gatewayTransactionId, Guid updatedBy)
    {
        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayTransactionId;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch(updatedBy);
    }

    public void MarkAsFailed(string? failureReason, Guid updatedBy)
    {
        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
        Touch(updatedBy);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Touch(Guid updatedBy)
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
