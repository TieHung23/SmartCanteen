using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.WalletTransaction.Enum;

namespace SC.Domain.Domain.WalletTransaction.Entity;

public class WalletTransaction : Entity<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private WalletTransaction() { }

    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public WalletTransactionType TransactionType { get; private set; }
    public Guid? PaymentId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    public static WalletTransaction Create(
        Guid userId,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        WalletTransactionType transactionType,
        Guid? paymentId = null)
    {
        return new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            TransactionType = transactionType,
            PaymentId = paymentId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
