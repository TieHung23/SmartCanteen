using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.WalletTransaction.Enum;
using WalletTransactionEntity = SC.Domain.Domain.WalletTransaction.Entity.WalletTransaction;

namespace SC.Application.MediatR.WalletTransaction.GetMyWalletTransactions;

internal sealed class GetMyWalletTransactionsQueryHandler(
    IGenericRepository<WalletTransactionEntity, Guid> walletTransactionRepository,
    ICurrentUserService currentUserService,
    ILogger<GetMyWalletTransactionsQueryHandler> logger)
    : IQueryHandler<GetMyWalletTransactionsQuery, PaginatedList<GetMyWalletTransactionsResponse>>
{
    public async Task<Result<PaginatedList<GetMyWalletTransactionsResponse>>> Handle(
        GetMyWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return Result.Failure<PaginatedList<GetMyWalletTransactionsResponse>>(
                    Error.Forbidden,
                    "Not authenticated.");
            }

            var transactions = await walletTransactionRepository
                .FindListAsync(transaction =>
                    !transaction.IsDeleted
                    && transaction.UserId == userId,
                    cancellationToken);

            var filtered = transactions.AsEnumerable();

            if (request.TransactionType.HasValue)
            {
                if (!Enum.IsDefined(typeof(WalletTransactionType), request.TransactionType.Value))
                {
                    return Result.Failure<PaginatedList<GetMyWalletTransactionsResponse>>(
                        Error.InvalidValue,
                        "Wallet transaction type is invalid.");
                }

                filtered = filtered.Where(
                    transaction => (int)transaction.TransactionType == request.TransactionType.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var pageItems = filteredList
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .Select(transaction => new GetMyWalletTransactionsResponse
                {
                    Id = transaction.Id,
                    Amount = transaction.Amount,
                    BalanceBefore = transaction.BalanceBefore,
                    BalanceAfter = transaction.BalanceAfter,
                    TransactionType = (int)transaction.TransactionType,
                    TransactionTypeName = transaction.TransactionType.ToString(),
                    PaymentId = transaction.PaymentId,
                    CreatedAtUtc = transaction.CreatedAtUtc
                })
                .ToList();

            return Result.Success(
                new PaginatedList<GetMyWalletTransactionsResponse>(
                    pageItems,
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Wallet transactions retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving wallet transactions for current user");
            return Result.Failure<PaginatedList<GetMyWalletTransactionsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving wallet transactions.");
        }
    }
}
