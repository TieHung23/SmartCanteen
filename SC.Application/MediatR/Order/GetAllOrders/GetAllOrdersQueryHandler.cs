using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.GetAllOrders;

internal class GetAllOrdersQueryHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ICurrentUserService currentUserService,
    ILogger<GetAllOrdersQueryHandler> logger
) : IQueryHandler<GetAllOrdersQuery, PaginatedList<GetAllOrdersResponse>>
{
    public async Task<Result<PaginatedList<GetAllOrdersResponse>>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allOrders = await orderRepository.FindListAsync(cancellationToken: cancellationToken);
            var filtered = allOrders.AsEnumerable();

            // If no specific user is requested, filter to current user
            Guid filterUserId = request.UserId ?? currentUserService.UserId;
            filtered = filtered.Where(x => x.CreatedBy == filterUserId);

            if (request.SessionId.HasValue)
            {
                filtered = filtered.Where(x => x.SessionId == request.SessionId.Value);
            }

            if (request.Status.HasValue)
            {
                if (!Enum.IsDefined(typeof(OrderStatus), request.Status.Value))
                {
                    return Result.Failure<PaginatedList<GetAllOrdersResponse>>(
                        Error.InvalidValue,
                        "Order status is invalid.");
                }

                filtered = filtered.Where(x => (int)x.Status == request.Status.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;

            var skipCount = request.GetSkipCount();
            var paginatedOrders = filteredList
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedOrders.Select(o => new GetAllOrdersResponse
            {
                Id = o.Id,
                SessionId = o.SessionId,
                MealTemplateId = o.MealTemplateId,
                TransactionId = o.WalletTransactionId,
                UserId = o.CreatedBy,
                Status = (int)o.Status,
                TotalPrice = o.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity),
                ItemCount = o.OrderItems.Count,
                CreatedAtUtc = o.CreatedAtUtc
            }).ToList();

            var paginatedResult = new PaginatedList<GetAllOrdersResponse>(
                responses,
                request.PageNumber,
                request.PageSize,
                totalCount);

            return Result.Success(paginatedResult, "Orders retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders");
            return Result.Failure<PaginatedList<GetAllOrdersResponse>>(
                Error.ServerError,
                "An error occurred while retrieving orders.");
        }
    }
}
