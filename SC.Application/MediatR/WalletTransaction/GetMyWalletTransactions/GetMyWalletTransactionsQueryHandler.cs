using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.WalletTransaction.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using RefundRequestAggregate = SC.Domain.Domain.Refund.AggregateRoot.RefundRequest;
using WalletTransactionEntity = SC.Domain.Domain.WalletTransaction.Entity.WalletTransaction;

namespace SC.Application.MediatR.WalletTransaction.GetMyWalletTransactions;

internal sealed class GetMyWalletTransactionsQueryHandler(
    IGenericRepository<WalletTransactionEntity, Guid> walletTransactionRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<RefundRequestAggregate, Guid> refundRepository,
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
            var pageTransactions = filteredList
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

            var orderIdByTransaction = await ResolveOrderIdsAsync(pageTransactions, cancellationToken);

            var pageItems = pageTransactions
                .Select(transaction => new GetMyWalletTransactionsResponse
                {
                    Id = transaction.Id,
                    Amount = transaction.Amount,
                    BalanceBefore = transaction.BalanceBefore,
                    BalanceAfter = transaction.BalanceAfter,
                    TransactionType = (int)transaction.TransactionType,
                    TransactionTypeName = transaction.TransactionType.ToString(),
                    PaymentId = transaction.PaymentId,
                    OrderId = orderIdByTransaction.TryGetValue(transaction.Id, out var orderId)
                        ? orderId
                        : null,
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

    /// <summary>
    /// A wallet transaction stores no order reference of its own - the link is owned by the other
    /// side (Order.WalletTransactionId for a payment, RefundRequest.WalletTransactionId for a
    /// refund), both unique. So the order id is resolved by looking those up for the current page
    /// only, rather than denormalising a column that could drift from the links it copies.
    /// Soft-deleted orders/refunds are included on purpose: a past transaction still has to say
    /// which order it belonged to.
    /// </summary>
    private async Task<Dictionary<Guid, Guid>> ResolveOrderIdsAsync(
        IReadOnlyCollection<WalletTransactionEntity> transactions,
        CancellationToken cancellationToken)
    {
        var orderIdByTransaction = new Dictionary<Guid, Guid>();

        var paymentTransactionIds = transactions
            .Where(transaction => transaction.TransactionType == WalletTransactionType.OrderPayment)
            .Select(transaction => transaction.Id)
            .ToList();

        if (paymentTransactionIds.Count > 0)
        {
            var orders = await orderRepository.FindListAsync(
                order => order.WalletTransactionId.HasValue
                         && paymentTransactionIds.Contains(order.WalletTransactionId.Value),
                cancellationToken);

            foreach (var order in orders)
            {
                if (order.WalletTransactionId is Guid transactionId)
                    orderIdByTransaction[transactionId] = order.Id;
            }
        }

        var refundTransactionIds = transactions
            .Where(transaction => transaction.TransactionType == WalletTransactionType.Refund)
            .Select(transaction => transaction.Id)
            .ToList();

        if (refundTransactionIds.Count > 0)
        {
            var refunds = await refundRepository.FindListAsync(
                refund => refund.WalletTransactionId.HasValue
                          && refundTransactionIds.Contains(refund.WalletTransactionId.Value),
                cancellationToken);

            foreach (var refund in refunds)
            {
                if (refund.WalletTransactionId is Guid transactionId)
                    orderIdByTransaction[transactionId] = refund.OrderId;
            }
        }

        return orderIdByTransaction;
    }
}
