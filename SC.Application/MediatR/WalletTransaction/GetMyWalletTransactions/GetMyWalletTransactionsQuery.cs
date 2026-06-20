using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;

namespace SC.Application.MediatR.WalletTransaction.GetMyWalletTransactions;

public sealed class GetMyWalletTransactionsQuery
    : PaginationParams, IQuery<PaginatedList<GetMyWalletTransactionsResponse>>
{
    public int? TransactionType { get; set; }
}
