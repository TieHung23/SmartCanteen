using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using DishAggregate = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregate = SC.Domain.Domain.Session.AggregateRoot.Session;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Reports.Manager.GetSessionDetailReport;

internal sealed class GetSessionDetailReportQueryHandler(
    IGenericRepository<SessionAggregate, Guid> sessionRepository,
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<DishAggregate, Guid> dishRepository,
    ILogger<GetSessionDetailReportQueryHandler> logger)
    : IQueryHandler<GetSessionDetailReportQuery, GetSessionDetailReportResponse>
{
    private static readonly TimeSpan TrendBucketSize = TimeSpan.FromMinutes(30);

    public async Task<Result<GetSessionDetailReportResponse>> Handle(
        GetSessionDetailReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.SessionId == Guid.Empty)
            {
                return Result.Failure<GetSessionDetailReportResponse>(
                    Error.InvalidValue,
                    "Session id is required.");
            }

            var session = await sessionRepository.FindSingleAsync(
                session => session.Id == request.SessionId && !session.IsDeleted,
                cancellationToken);
            if (session is null)
            {
                return Result.Failure<GetSessionDetailReportResponse>(
                    Error.SessionNotFound,
                    "Session was not found.");
            }

            var orders = await orderRepository.FindListAsync(
                order => order.SessionId == request.SessionId && !order.IsDeleted,
                query => query.Include(order => order.OrderItems),
                cancellationToken);
            var orderIds = orders.Select(order => order.Id).ToList();

            List<RefundRequest> refunds = orderIds.Count == 0
                ? []
                : await refundRepository.FindListAsync(
                    refund => orderIds.Contains(refund.OrderId) && !refund.IsDeleted,
                    cancellationToken);

            var userIds = orders.Select(order => order.CreatedBy).Distinct().ToList();
            List<UserAggregate> users = userIds.Count == 0
                ? []
                : await userRepository.FindListAsync(
                    user => userIds.Contains(user.Id) && !user.IsDeleted,
                    cancellationToken);

            var dishIds = orders
                .SelectMany(order => order.OrderItems)
                .Select(item => item.DishId)
                .Distinct()
                .ToList();
            List<DishAggregate> dishes = dishIds.Count == 0
                ? []
                : await dishRepository.FindListAsync(
                    dish => dishIds.Contains(dish.Id) && !dish.IsDeleted,
                    cancellationToken);

            var completedOrders = orders
                .Where(order => order.Status == OrderStatus.Completed)
                .ToList();
            var approvedRefunds = refunds
                .Where(refund => refund.Status == RefundRequestStatus.Approved)
                .ToList();
            var totalRevenue = completedOrders.Sum(CalculateOrderAmount);

            var response = new GetSessionDetailReportResponse
            {
                Session = BuildSessionInfo(session),
                Summary = new SessionDetailSummaryResponse
                {
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(order => order.Status == OrderStatus.Pending),
                    PreparingOrders = orders.Count(order => order.Status == OrderStatus.Preparing),
                    ReadyForPickupOrders = orders.Count(order => order.Status == OrderStatus.ReadyForPickup),
                    CompletedOrders = completedOrders.Count,
                    CancelledOrders = orders.Count(order => order.Status == OrderStatus.Cancelled),
                    ExpiredOrders = orders.Count(order => order.Status == OrderStatus.Expired),
                    TotalRevenue = totalRevenue,
                    RefundRequests = refunds.Count,
                    ApprovedRefunds = approvedRefunds.Count,
                    RejectedRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Rejected),
                    PendingRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Pending),
                    RefundAmount = approvedRefunds.Sum(refund => refund.RefundAmount),
                    CompletionRate = CalculateRate(completedOrders.Count, orders.Count),
                    CancelRate = CalculateRate(
                        orders.Count(order => order.Status == OrderStatus.Cancelled),
                        orders.Count),
                    RefundRate = CalculateRate(approvedRefunds.Count, completedOrders.Count),
                    AverageOrderValue = CalculateAverage(totalRevenue, completedOrders.Count)
                },
                Timeline = BuildTimeline(session),
                OrderTrend = BuildOrderTrend(session, orders),
                OrderStats = BuildOrderStats(orders),
                PopularDishes = BuildPopularDishes(completedOrders, dishes),
                RecentOrders = BuildRecentOrders(orders, users)
            };

            return Result.Success(response, "Session detail report retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving session detail report for session {SessionId}",
                request.SessionId);
            return Result.Failure<GetSessionDetailReportResponse>(
                Error.ServerError,
                "An error occurred while retrieving session detail report.");
        }
    }

    private static SessionDetailInfoResponse BuildSessionInfo(SessionAggregate session)
    {
        return new SessionDetailInfoResponse
        {
            SessionId = session.Id,
            SessionName = session.Name,
            Description = session.Description,
            IsActive = session.IsActive,
            IsFinalized = session.IsFinalized,
            AvailableForOrder = session.AvailableForOrder,
            AvailableFrom = session.AvailableFrom,
            AvailableTo = session.AvailableTo,
            FinalizationDeadline = session.FinalizationDeadline,
            FinalizedAtUtc = session.FinalizedAtUtc
        };
    }

    private static IReadOnlyList<SessionTimelineItemResponse> BuildTimeline(SessionAggregate session)
    {
        var items = new List<SessionTimelineItemResponse>
        {
            new()
            {
                Time = session.AvailableForOrder,
                Label = "Orders opened",
                Type = "orders_opened"
            },
            new()
            {
                Time = session.AvailableFrom,
                Label = "Serving started",
                Type = "serving_started"
            },
            new()
            {
                Time = session.AvailableTo,
                Label = "Serving ended",
                Type = "serving_ended"
            }
        };

        if (session.FinalizationDeadline.HasValue)
        {
            items.Add(new SessionTimelineItemResponse
            {
                Time = session.FinalizationDeadline.Value,
                Label = "Finalization deadline",
                Type = "finalization_deadline"
            });
        }

        if (session.FinalizedAtUtc.HasValue)
        {
            items.Add(new SessionTimelineItemResponse
            {
                Time = session.FinalizedAtUtc.Value,
                Label = "Finalized",
                Type = "finalized"
            });
        }

        return items
            .OrderBy(item => item.Time)
            .ToList();
    }

    private static IReadOnlyList<SessionOrderTrendResponse> BuildOrderTrend(
        SessionAggregate session,
        IReadOnlyCollection<OrderAggregate> orders)
    {
        var grouped = orders
            .GroupBy(order => FloorToBucket(order.CreatedAtUtc, TrendBucketSize))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Orders = group.Count(),
                    Revenue = group
                        .Where(order => order.Status == OrderStatus.Completed)
                        .Sum(CalculateOrderAmount)
                });

        if (session.AvailableTo <= session.AvailableForOrder
            || session.AvailableTo - session.AvailableForOrder > TimeSpan.FromDays(2))
        {
            return grouped
                .OrderBy(group => group.Key)
                .Select(group => new SessionOrderTrendResponse
                {
                    TimeBucket = group.Key,
                    Orders = group.Value.Orders,
                    Revenue = group.Value.Revenue
                })
                .ToList();
        }

        var result = new List<SessionOrderTrendResponse>();
        for (var bucket = FloorToBucket(session.AvailableForOrder, TrendBucketSize);
             bucket <= session.AvailableTo;
             bucket = bucket.Add(TrendBucketSize))
        {
            grouped.TryGetValue(bucket, out var value);
            result.Add(new SessionOrderTrendResponse
            {
                TimeBucket = bucket,
                Orders = value?.Orders ?? 0,
                Revenue = value?.Revenue ?? 0
            });
        }

        return result;
    }

    private static IReadOnlyList<SessionOrderStatResponse> BuildOrderStats(
        IReadOnlyCollection<OrderAggregate> orders)
    {
        return Enum.GetValues<OrderStatus>()
            .Select(status => new SessionOrderStatResponse
            {
                Status = (int)status,
                Label = status.ToString(),
                Count = orders.Count(order => order.Status == status)
            })
            .ToList();
    }

    private static IReadOnlyList<SessionPopularDishResponse> BuildPopularDishes(
        IReadOnlyCollection<OrderAggregate> completedOrders,
        IReadOnlyCollection<DishAggregate> dishes)
    {
        var dishMap = dishes.ToDictionary(dish => dish.Id);

        return completedOrders
            .SelectMany(order => order.OrderItems.Select(item => new
            {
                OrderId = order.Id,
                item.DishId,
                item.Quantity,
                Revenue = item.UnitPrice.Amount * item.Quantity
            }))
            .GroupBy(item => item.DishId)
            .Select(group =>
            {
                dishMap.TryGetValue(group.Key, out var dish);
                return new SessionPopularDishResponse
                {
                    DishId = group.Key,
                    DishName = dish?.Name ?? string.Empty,
                    ImgUrl = dish?.ImgUrl,
                    TotalOrders = group.Select(item => item.OrderId).Distinct().Count(),
                    TotalQuantity = group.Sum(item => item.Quantity),
                    Revenue = group.Sum(item => item.Revenue)
                };
            })
            .OrderByDescending(dish => dish.TotalQuantity)
            .ThenByDescending(dish => dish.Revenue)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<SessionRecentOrderResponse> BuildRecentOrders(
        IReadOnlyCollection<OrderAggregate> orders,
        IReadOnlyCollection<UserAggregate> users)
    {
        var userMap = users.ToDictionary(user => user.Id);

        return orders
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(20)
            .Select(order =>
            {
                userMap.TryGetValue(order.CreatedBy, out var user);
                return new SessionRecentOrderResponse
                {
                    OrderId = order.Id,
                    UserId = order.CreatedBy,
                    CustomerName = user?.Name ?? string.Empty,
                    CustomerEmail = user?.Email ?? string.Empty,
                    Status = (int)order.Status,
                    TotalPrice = CalculateOrderAmount(order),
                    ItemCount = order.OrderItems.Count,
                    CreatedAtUtc = order.CreatedAtUtc
                };
            })
            .ToList();
    }

    private static DateTimeOffset FloorToBucket(DateTimeOffset value, TimeSpan bucketSize)
    {
        var ticks = value.Ticks - value.Ticks % bucketSize.Ticks;
        return new DateTimeOffset(ticks, value.Offset);
    }

    private static decimal CalculateOrderAmount(OrderAggregate order)
    {
        return order.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);
    }

    private static decimal CalculateRate(decimal numerator, decimal denominator)
    {
        if (denominator == 0)
        {
            return 0;
        }

        return decimal.Round(numerator / denominator * 100, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateAverage(decimal total, int count)
    {
        if (count == 0)
        {
            return 0;
        }

        return decimal.Round(total / count, 2, MidpointRounding.AwayFromZero);
    }
}
