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

    /// <summary>
    /// Order this transaction belongs to: the order that was paid for (OrderPayment) or the order a
    /// refund was issued against (Refund). Null for TopUp, which is not tied to any order.
    /// </summary>
    public Guid? OrderId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
