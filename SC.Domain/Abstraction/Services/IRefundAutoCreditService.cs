using SC.Contract.Shared;
using SC.Domain.Domain.Refund.AggregateRoot;

namespace SC.Domain.Abstraction.Services;

public interface IRefundAutoCreditService
{
    Task<Result<RefundAutoCreditResult>> CreditAsync(
        RefundRequest refund,
        CancellationToken cancellationToken = default);
}

public readonly record struct RefundAutoCreditResult(Guid WalletTransactionId, decimal BalanceAfter);
