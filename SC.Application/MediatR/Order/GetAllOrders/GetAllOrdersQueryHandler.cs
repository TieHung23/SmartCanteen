using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
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
            IQueryable<OrderAggregateRoot> query = orderRepository.GetQueryable();

            // If no specific user is requested, filter to current user
            Guid filterUserId = request.UserId ?? currentUserService.UserId;
            query = query.Where(x => x.CreatedBy == filterUserId);

            if (request.Status.HasValue)
            {
                query = query.Where(x => (int)x.Status == request.Status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var skipCount = request.GetSkipCount();
            var paginatedOrders = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(skipCount)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = paginatedOrders.Select(o => new GetAllOrdersResponse
            {
                Id = o.Id,
                MealId = o.MealId,
                PaymentId = o.PaymentId,
                UserId = o.CreatedBy,
                Status = (int)o.Status,
                TotalPrice = o.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity),
                Currency = o.OrderItems.FirstOrDefault()?.UnitPrice.Currency ?? "VND",
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
