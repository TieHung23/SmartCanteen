using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using SessionAggregate = SC.Domain.Domain.Session.AggregateRoot.Session;

namespace SC.Application.MediatR.Reports.Manager.GetSessionReport;

internal sealed class GetSessionReportQueryHandler(
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SessionAggregate, Guid> sessionRepository,
    ILogger<GetSessionReportQueryHandler> logger)
    : IQueryHandler<GetSessionReportQuery, GetSessionReportResponse>
{
    public async Task<Result<GetSessionReportResponse>> Handle(
        GetSessionReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rangeResult = ReportDateRange.Create(request.From, request.To);
            if (rangeResult.IsFailure)
            {
                return Result.Failure<GetSessionReportResponse>(
                    rangeResult.Error!,
                    rangeResult.Message);
            }

            var range = rangeResult.Value!;
            var orders = await orderRepository.FindListAsync(
                order => !order.IsDeleted,
                query => query.Include(order => order.OrderItems),
                cancellationToken);
            var refunds = await refundRepository.FindListAsync(
                refund => !refund.IsDeleted,
                cancellationToken);
            var sessions = await sessionRepository.FindListAsync(
                session => !session.IsDeleted,
                cancellationToken);

            var ordersInRange = orders
                .Where(order => ReportDateRange.Contains(order.CreatedAtUtc, range))
                .ToList();
            var orderMap = orders.ToDictionary(order => order.Id);
            var refundsInRange = refunds
                .Where(refund => ReportDateRange.Contains(refund.CreatedAtUtc, range))
                .ToList();
            var refundsBySession = refundsInRange
                .Where(refund => orderMap.ContainsKey(refund.OrderId))
                .GroupBy(refund => orderMap[refund.OrderId].SessionId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var sessionMap = sessions.ToDictionary(session => session.Id);
            var items = ordersInRange
                .GroupBy(order => order.SessionId)
                .Select(group =>
                {
                    var sessionOrders = group.ToList();
                    refundsBySession.TryGetValue(group.Key, out var sessionRefunds);
                    sessionRefunds ??= [];
                    sessionMap.TryGetValue(group.Key, out var session);

                    var completedOrders = sessionOrders.Count(order => order.Status == OrderStatus.Completed);
                    var approvedRefunds = sessionRefunds.Count(refund => refund.Status == RefundRequestStatus.Approved);

                    return new SessionReportItem
                    {
                        SessionId = group.Key,
                        SessionName = session?.Name ?? string.Empty,
                        AvailableFrom = session?.AvailableFrom ?? default,
                        AvailableTo = session?.AvailableTo ?? default,
                        TotalOrders = sessionOrders.Count,
                        CompletedOrders = completedOrders,
                        CancelledOrders = sessionOrders.Count(order => order.Status == OrderStatus.Cancelled),
                        ExpiredOrders = sessionOrders.Count(order => order.Status == OrderStatus.Expired),
                        Revenue = sessionOrders
                            .Where(order => order.Status == OrderStatus.Completed)
                            .Sum(CalculateOrderAmount),
                        RefundRequests = sessionRefunds.Count,
                        RefundAmount = sessionRefunds
                            .Where(refund => refund.Status == RefundRequestStatus.Approved)
                            .Sum(refund => refund.RefundAmount),
                        CompletionRate = CalculateRate(completedOrders, sessionOrders.Count),
                        RefundRate = CalculateRate(approvedRefunds, completedOrders)
                    };
                })
                .OrderByDescending(item => item.Revenue)
                .ThenByDescending(item => item.TotalOrders)
                .ToList();

            return Result.Success(
                new GetSessionReportResponse
                {
                    Range = ReportDateRange.ToResponse(range),
                    Items = items
                },
                "Session report retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving session report");
            return Result.Failure<GetSessionReportResponse>(
                Error.ServerError,
                "An error occurred while retrieving session report.");
        }
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
}
