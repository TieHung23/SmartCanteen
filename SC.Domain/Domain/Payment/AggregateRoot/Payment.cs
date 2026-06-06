using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.Payment.Enum;
using SC.Domain.Domain.Payment.ValueObject;

namespace SC.Domain.Domain.Payment.AggregateRoot;

public class Payment : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private Payment()
    {
    }

    public required BalanceSnapshot BalanceSnapshot { get; set; }
    public required string GatewayOrderId { get; set; }
    public string? GatewayTransactionId { get; set; }
    public decimal AmountVnd { get; set; }
    public decimal ConvertedPoints { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentType Type { get; set; }
    public string? FailureReason { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Payment Create(
        BalanceSnapshot balanceSnapshot,
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
            BalanceSnapshot = balanceSnapshot,
            GatewayOrderId = gatewayOrderId,
            AmountVnd = amountVnd,
            ConvertedPoints = convertedPoints,
            Status = PaymentStatus.Pending,
            Method = method,
            UserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            Type = type
        };
    }

    public void MarkAsPending()
    {
        Status = PaymentStatus.Pending;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkAsCompleted(string? gatewayTransactionId, Guid updatedBy)
    {
        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayTransactionId;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void MarkAsFailed(string? failureReason, Guid updatedBy)
    {
        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
