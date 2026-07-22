using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SC.Domain.Domain.User.Enum;
using DishAggregate = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregate = SC.Domain.Domain.Session.AggregateRoot.Session;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Reports.Manager.GetReportSummary;

internal sealed class GetReportSummaryQueryHandler(
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<SessionAggregate, Guid> sessionRepository,
    IGenericRepository<DishAggregate, Guid> dishRepository,
    ILogger<GetReportSummaryQueryHandler> logger)
    : IQueryHandler<GetReportSummaryQuery, GetReportSummaryResponse>
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string ReportTimezone = "Asia/Ho_Chi_Minh";

    public async Task<Result<GetReportSummaryResponse>> Handle(
        GetReportSummaryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeZone = GetReportTimeZone();
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).Date);
            var from = today.AddDays(1 - today.Day);
            var to = today;

            if (!string.IsNullOrWhiteSpace(request.From)
                && !DateOnly.TryParseExact(request.From, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out from))
            {
                return Result.Failure<GetReportSummaryResponse>(
                    Error.InvalidValue,
                    "From must use yyyy-MM-dd format.");
            }

            if (!string.IsNullOrWhiteSpace(request.To)
                && !DateOnly.TryParseExact(request.To, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out to))
            {
                return Result.Failure<GetReportSummaryResponse>(
                    Error.InvalidValue,
                    "To must use yyyy-MM-dd format.");
            }

            if (from > to)
            {
                return Result.Failure<GetReportSummaryResponse>(
                    Error.InvalidValue,
                    "From must be earlier than or equal to To.");
            }

            var currentRange = CreateRange(from, to, timeZone);
            var previousRange = CreatePreviousRange(from, to, timeZone);

            var orders = await orderRepository.FindListAsync(
                order => !order.IsDeleted,
                query => query.Include(order => order.OrderItems),
                cancellationToken);
            var refunds = await refundRepository.FindListAsync(
                refund => !refund.IsDeleted,
                cancellationToken);
            var users = await userRepository.FindListAsync(
                user => !user.IsDeleted,
                cancellationToken);
            var sessions = await sessionRepository.FindListAsync(
                session => !session.IsDeleted,
                cancellationToken);
            var dishes = await dishRepository.FindListAsync(
                dish => !dish.IsDeleted,
                cancellationToken);

            var current = BuildSnapshot(orders, refunds, users, currentRange);
            var previous = BuildSnapshot(orders, refunds, users, previousRange);
            var completedOrders = current.Orders
                .Where(order => order.Status == OrderStatus.Completed)
                .ToList();
            var refundStats = BuildRefundStats(current.Refunds, current.CompletedOrderCount);
            var popularDishes = BuildPopularDishes(completedOrders, dishes);
            var topDish = popularDishes.FirstOrDefault()?.DishName ?? string.Empty;

            var response = new GetReportSummaryResponse
            {
                Range = new ReportRangeResponse
                {
                    From = from.ToString(DateFormat, CultureInfo.InvariantCulture),
                    To = to.ToString(DateFormat, CultureInfo.InvariantCulture),
                    Timezone = ReportTimezone
                },
                Dashboard = new ReportDashboardResponse
                {
                    TotalOrders = current.TotalOrders,
                    TotalRevenue = current.TotalRevenue,
                    RefundRate = current.RefundRate,
                    TopDish = topDish,
                    NewCustomers = current.NewCustomers,
                    TotalComplaints = current.TotalRefunds,
                    ActiveSessions = CountActiveSessions(sessions),
                    OrderChange = CalculateChange(current.TotalOrders, previous.TotalOrders),
                    RevenueChange = CalculateChange(current.TotalRevenue, previous.TotalRevenue),
                    RefundChange = CalculateChange(current.RefundRate, previous.RefundRate),
                    CustomerChange = CalculateChange(current.NewCustomers, previous.NewCustomers)
                },
                RevenueTrend = BuildRevenueTrend(completedOrders, from, to, timeZone),
                PopularDishes = popularDishes,
                OrderStats = BuildOrderStats(current.Orders),
                RefundStats = refundStats
            };

            return Result.Success(response, "Report summary retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving report summary");
            return Result.Failure<GetReportSummaryResponse>(
                Error.ServerError,
                "An error occurred while retrieving report summary.");
        }
    }

    private static ReportSnapshot BuildSnapshot(
        IReadOnlyCollection<OrderAggregate> allOrders,
        IReadOnlyCollection<RefundRequest> allRefunds,
        IReadOnlyCollection<UserAggregate> allUsers,
        RangeBounds range)
    {
        var orders = allOrders
            .Where(order => IsInRange(order.CreatedAtUtc, range))
            .ToList();
        var completedOrders = orders
            .Where(order => order.Status == OrderStatus.Completed)
            .ToList();
        var refunds = allRefunds
            .Where(refund => IsInRange(refund.CreatedAtUtc, range))
            .ToList();
        var approvedRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Approved);
        var newCustomers = allUsers.Count(user =>
            user.Role == Role.User
            && IsInRange(user.CreatedAtUtc, range));
        var totalRevenue = completedOrders.Sum(CalculateOrderAmount);
        var refundRate = CalculateRate(approvedRefunds, completedOrders.Count);

        return new ReportSnapshot(
            orders,
            refunds,
            orders.Count,
            completedOrders.Count,
            totalRevenue,
            refunds.Count,
            newCustomers,
            refundRate);
    }

    private static IReadOnlyList<RevenueTrendResponse> BuildRevenueTrend(
        IReadOnlyCollection<OrderAggregate> completedOrders,
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone)
    {
        var ordersByDate = completedOrders
            .GroupBy(order => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(order.CreatedAtUtc, timeZone).Date))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Revenue = group.Sum(CalculateOrderAmount),
                    Orders = group.Count()
                });

        var result = new List<RevenueTrendResponse>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            ordersByDate.TryGetValue(date, out var value);
            result.Add(new RevenueTrendResponse
            {
                Date = date.ToString(DateFormat, CultureInfo.InvariantCulture),
                Revenue = value?.Revenue ?? 0,
                Orders = value?.Orders ?? 0
            });
        }

        return result;
    }

    private static IReadOnlyList<PopularDishResponse> BuildPopularDishes(
        IReadOnlyCollection<OrderAggregate> completedOrders,
        IReadOnlyCollection<DishAggregate> dishes)
    {
        var dishMap = dishes.ToDictionary(dish => dish.Id, dish => dish.Name);

        return completedOrders
            .SelectMany(order => order.OrderItems.Select(item => new
            {
                OrderId = order.Id,
                item.DishId,
                item.Quantity,
                Revenue = item.UnitPrice.Amount * item.Quantity
            }))
            .GroupBy(item => item.DishId)
            .Select(group => new PopularDishResponse
            {
                DishId = group.Key,
                DishName = dishMap.GetValueOrDefault(group.Key) ?? string.Empty,
                TotalOrders = group.Select(item => item.OrderId).Distinct().Count(),
                TotalQuantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.Revenue)
            })
            .OrderByDescending(dish => dish.TotalQuantity)
            .ThenByDescending(dish => dish.Revenue)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<OrderStatResponse> BuildOrderStats(
        IReadOnlyCollection<OrderAggregate> orders)
    {
        return Enum.GetValues<OrderStatus>()
            .Select(status => new OrderStatResponse
            {
                Status = (int)status,
                Label = status.ToString(),
                Count = orders.Count(order => order.Status == status)
            })
            .ToList();
    }

    private static RefundStatsResponse BuildRefundStats(
        IReadOnlyCollection<RefundRequest> refunds,
        int completedOrderCount)
    {
        var totalRefunds = refunds.Count;
        var approvedRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Approved);

        return new RefundStatsResponse
        {
            TotalRefunds = totalRefunds,
            TotalRefundAmount = refunds
                .Where(refund => refund.Status == RefundRequestStatus.Approved)
                .Sum(refund => refund.RefundAmount),
            ApprovedRefunds = approvedRefunds,
            RejectedRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Rejected),
            PendingRefunds = refunds.Count(refund => refund.Status == RefundRequestStatus.Pending),
            RefundRate = CalculateRate(approvedRefunds, completedOrderCount),
            Breakdown = BuildRefundBreakdown(refunds)
        };
    }

    private static IReadOnlyList<RefundBreakdownResponse> BuildRefundBreakdown(
        IReadOnlyCollection<RefundRequest> refunds)
    {
        return
        [
            BuildRefundBreakdownItem(refunds, "manual", "Manual refund", IsManualRefund),
            BuildRefundBreakdownItem(refunds, "item", "Item refund", IsItemRefund),
            BuildRefundBreakdownItem(refunds, "order", "Order refund", IsOrderRefund)
        ];
    }

    private static RefundBreakdownResponse BuildRefundBreakdownItem(
        IReadOnlyCollection<RefundRequest> refunds,
        string type,
        string label,
        Func<RefundRequest, bool> predicate)
    {
        var matched = refunds.Where(predicate).ToList();
        return new RefundBreakdownResponse
        {
            Type = type,
            Label = label,
            Count = matched.Count,
            Amount = matched.Sum(refund => refund.RefundAmount),
            Rate = CalculateRate(matched.Count, refunds.Count)
        };
    }

    private static int CountActiveSessions(IReadOnlyCollection<SessionAggregate> sessions)
    {
        var now = DateTimeOffset.UtcNow;
        return sessions.Count(session =>
            session.IsActive
            && now >= session.AvailableForOrder
            && now <= session.AvailableTo);
    }

    private static bool IsManualRefund(RefundRequest refund)
    {
        return !refund.ChangeProposalId.HasValue;
    }

    private static bool IsItemRefund(RefundRequest refund)
    {
        return refund.ChangeProposalId.HasValue && refund.OrderItemId.HasValue;
    }

    private static bool IsOrderRefund(RefundRequest refund)
    {
        return refund.ChangeProposalId.HasValue && !refund.OrderItemId.HasValue;
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

    private static decimal CalculateChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return 0;
        }

        return decimal.Round((current - previous) / previous * 100, 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsInRange(DateTimeOffset value, RangeBounds range)
    {
        return value >= range.StartUtc && value < range.EndUtcExclusive;
    }

    private static RangeBounds CreatePreviousRange(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone)
    {
        var days = to.DayNumber - from.DayNumber + 1;
        var previousFrom = from.AddDays(-days);
        var previousTo = from.AddDays(-1);
        return CreateRange(previousFrom, previousTo, timeZone);
    }

    private static RangeBounds CreateRange(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo timeZone)
    {
        var startUtc = ToUtcStartOfDay(from, timeZone);
        var endUtcExclusive = ToUtcStartOfDay(to.AddDays(1), timeZone);
        return new RangeBounds(startUtc, endUtcExclusive);
    }

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date, TimeZoneInfo timeZone)
    {
        var localDateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    private static TimeZoneInfo GetReportTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ReportTimezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private sealed record RangeBounds(
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtcExclusive);

    private sealed record ReportSnapshot(
        IReadOnlyList<OrderAggregate> Orders,
        IReadOnlyList<RefundRequest> Refunds,
        int TotalOrders,
        int CompletedOrderCount,
        decimal TotalRevenue,
        int TotalRefunds,
        int NewCustomers,
        decimal RefundRate);
}
