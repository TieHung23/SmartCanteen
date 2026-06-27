namespace SC.Application.MediatR.WalletTransaction.GetMyWalletTransactions;

public sealed class GetMyWalletTransactionsResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int TransactionType { get; set; }
    public string TransactionTypeName { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
