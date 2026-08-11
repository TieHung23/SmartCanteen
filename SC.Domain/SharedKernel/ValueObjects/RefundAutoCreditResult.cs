namespace SC.Domain.SharedKernel.ValueObjects;

public class RefundAutoCreditResult : Abstraction.Aggregates.ValueObject
{
    public RefundAutoCreditResult(Guid walletTransactionId, decimal balanceAfter)
    {
        WalletTransactionId = walletTransactionId;
        BalanceAfter = balanceAfter;
    }

    public Guid WalletTransactionId { get; }
    public decimal BalanceAfter { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return WalletTransactionId;
        yield return BalanceAfter;
    }
}
