using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.WalletTransaction.Enum;

namespace SC.Domain.Domain.WalletTransaction.Entity;

public class WalletTransaction : Entity<Guid>, IAuditableEntity<Guid>
{
    private WalletTransaction()
    {
    }

    public required Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public WalletTransactionType TransactionType { get; set; }
    public Guid? PaymentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

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
}
