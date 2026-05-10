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
    public required string GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentType Type { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static Payment Create(
        BalanceSnapshot balanceSnapshot,
        string gatewayTransactionId,
        PaymentMethod method,
        Guid userId,
        Guid createdBy,
        PaymentType type)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            BalanceSnapshot = balanceSnapshot,
            GatewayTransactionId = gatewayTransactionId,
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

    public void MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}