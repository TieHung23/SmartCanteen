using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Order.GetAllOrders;

internal class GetAllOrdersQueryHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
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

            if (request.CreatedFrom.HasValue)
            {
                filtered = filtered.Where(x => x.CreatedAtUtc >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                filtered = filtered.Where(x => x.CreatedAtUtc <= request.CreatedTo.Value);
            }

            var candidateOrders = filtered.ToList();

            var sessionIds = candidateOrders.Select(o => o.SessionId).Distinct().ToList();
            var sessions = await sessionRepository.FindListAsync(
                s => sessionIds.Contains(s.Id),
                cancellationToken);
            var sessionMap = sessions.ToDictionary(s => s.Id);

            if (request.SessionDateFrom.HasValue)
            {
                candidateOrders = candidateOrders
                    .Where(o => sessionMap.TryGetValue(o.SessionId, out var s)
                                && s.AvailableFrom >= request.SessionDateFrom.Value)
                    .ToList();
            }

            if (request.SessionDateTo.HasValue)
            {
                candidateOrders = candidateOrders
                    .Where(o => sessionMap.TryGetValue(o.SessionId, out var s)
                                && s.AvailableFrom <= request.SessionDateTo.Value)
                    .ToList();
            }

            var totalCount = candidateOrders.Count;

            var skipCount = request.GetSkipCount();
            var paginatedOrders = candidateOrders
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToList();

            var responses = paginatedOrders.Select(o => new GetAllOrdersResponse
            {
                Id = o.Id,
                SessionId = o.SessionId,
                SessionName = sessionMap.TryGetValue(o.SessionId, out var session) ? session.Name : string.Empty,
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
