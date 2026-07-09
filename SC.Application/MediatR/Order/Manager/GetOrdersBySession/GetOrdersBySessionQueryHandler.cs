using Microsoft.Extensions.Logging;
using SC.Application.MediatR.Order.GetAllOrders;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.Manager.GetOrdersBySession;

internal sealed class GetOrdersBySessionQueryHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ILogger<GetOrdersBySessionQueryHandler> logger)
    : IQueryHandler<GetOrdersBySessionQuery, PaginatedList<GetAllOrdersResponse>>
{
    public async Task<Result<PaginatedList<GetAllOrdersResponse>>> Handle(
        GetOrdersBySessionQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.SessionId == Guid.Empty)
            {
                return Result.Failure<PaginatedList<GetAllOrdersResponse>>(
                    Error.InvalidValue,
                    "Session id is required.");
            }

            if (request.Status.HasValue && !Enum.IsDefined(typeof(OrderStatus), request.Status.Value))
            {
                return Result.Failure<PaginatedList<GetAllOrdersResponse>>(
                    Error.InvalidValue,
                    "Order status is invalid.");
            }

            var orders = await orderRepository.FindListAsync(
                order => order.SessionId == request.SessionId,
                cancellationToken);

            var filtered = orders.AsEnumerable();
            if (request.Status.HasValue)
            {
                filtered = filtered.Where(order => (int)order.Status == request.Status.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var paginatedOrders = filteredList
                .OrderByDescending(order => order.CreatedAtUtc)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedOrders.Select(order => new GetAllOrdersResponse
            {
                Id = order.Id,
                SessionId = order.SessionId,
                MealTemplateId = order.MealTemplateId,
                TransactionId = order.WalletTransactionId,
                UserId = order.CreatedBy,
                Status = (int)order.Status,
                TotalPrice = order.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity),
                ItemCount = order.OrderItems.Count,
                CreatedAtUtc = order.CreatedAtUtc
            }).ToList();

            return Result.Success(
                new PaginatedList<GetAllOrdersResponse>(
                    responses,
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Orders retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving orders for session {SessionId}",
                request.SessionId);
            return Result.Failure<PaginatedList<GetAllOrdersResponse>>(
                Error.ServerError,
                "An error occurred while retrieving orders.");
        }
    }
}
