using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Reports.Manager.GetOrderIssuesReport;

internal sealed class GetOrderIssuesReportQueryHandler(
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    ILogger<GetOrderIssuesReportQueryHandler> logger)
    : IQueryHandler<GetOrderIssuesReportQuery, GetOrderIssuesReportResponse>
{
    public async Task<Result<GetOrderIssuesReportResponse>> Handle(
        GetOrderIssuesReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rangeResult = ReportDateRange.Create(request.From, request.To);
            if (rangeResult.IsFailure)
            {
                return Result.Failure<GetOrderIssuesReportResponse>(
                    rangeResult.Error!,
                    rangeResult.Message);
            }

            var range = rangeResult.Value!;
            var orders = await orderRepository.FindListAsync(
                order => !order.IsDeleted,
                cancellationToken);
            var refunds = await refundRepository.FindListAsync(
                refund => !refund.IsDeleted,
                cancellationToken);

            var ordersInRange = orders
                .Where(order => ReportDateRange.Contains(order.CreatedAtUtc, range))
                .ToList();
            var orderIdsInRange = ordersInRange.Select(order => order.Id).ToHashSet();
            var refundRequestedOrders = refunds
                .Where(refund =>
                    ReportDateRange.Contains(refund.CreatedAtUtc, range)
                    && orderIdsInRange.Contains(refund.OrderId))
                .Select(refund => refund.OrderId)
                .Distinct()
                .Count();

            var totalOrders = ordersInRange.Count;
            var cancelledOrders = ordersInRange.Count(order => order.Status == OrderStatus.Cancelled);
            var expiredOrders = ordersInRange.Count(order => order.Status == OrderStatus.Expired);

            var items = new List<OrderIssueReportItem>
            {
                new()
                {
                    Type = "cancelled",
                    Label = "Cancelled orders",
                    Count = cancelledOrders,
                    Rate = CalculateRate(cancelledOrders, totalOrders)
                },
                new()
                {
                    Type = "expired",
                    Label = "Expired orders",
                    Count = expiredOrders,
                    Rate = CalculateRate(expiredOrders, totalOrders)
                },
                new()
                {
                    Type = "refund_requested",
                    Label = "Refund requested orders",
                    Count = refundRequestedOrders,
                    Rate = CalculateRate(refundRequestedOrders, totalOrders)
                }
            };

            return Result.Success(
                new GetOrderIssuesReportResponse
                {
                    Range = ReportDateRange.ToResponse(range),
                    TotalOrders = totalOrders,
                    CancelledOrders = cancelledOrders,
                    ExpiredOrders = expiredOrders,
                    RefundRequestedOrders = refundRequestedOrders,
                    CancelledRate = CalculateRate(cancelledOrders, totalOrders),
                    ExpiredRate = CalculateRate(expiredOrders, totalOrders),
                    RefundRequestRate = CalculateRate(refundRequestedOrders, totalOrders),
                    Items = items
                },
                "Order issues report retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order issues report");
            return Result.Failure<GetOrderIssuesReportResponse>(
                Error.ServerError,
                "An error occurred while retrieving order issues report.");
        }
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
