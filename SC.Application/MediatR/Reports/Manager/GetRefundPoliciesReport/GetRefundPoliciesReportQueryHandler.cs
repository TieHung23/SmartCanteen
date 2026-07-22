using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;

namespace SC.Application.MediatR.Reports.Manager.GetRefundPoliciesReport;

internal sealed class GetRefundPoliciesReportQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    ILogger<GetRefundPoliciesReportQueryHandler> logger)
    : IQueryHandler<GetRefundPoliciesReportQuery, GetRefundPoliciesReportResponse>
{
    public async Task<Result<GetRefundPoliciesReportResponse>> Handle(
        GetRefundPoliciesReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rangeResult = ReportDateRange.Create(request.From, request.To);
            if (rangeResult.IsFailure)
            {
                return Result.Failure<GetRefundPoliciesReportResponse>(
                    rangeResult.Error!,
                    rangeResult.Message);
            }

            var range = rangeResult.Value!;
            var refunds = await refundRepository.FindListAsync(
                refund => !refund.IsDeleted,
                cancellationToken);
            var refundsInRange = refunds
                .Where(refund => ReportDateRange.Contains(refund.CreatedAtUtc, range))
                .ToList();

            var items = refundsInRange
                .GroupBy(refund => new
                {
                    refund.PolicyCode,
                    refund.PolicyNameSnapshot
                })
                .Select(group =>
                {
                    var policyRefunds = group.ToList();
                    var approvedRequests = policyRefunds.Count(refund => refund.Status == RefundRequestStatus.Approved);

                    return new RefundPolicyReportItem
                    {
                        PolicyCode = group.Key.PolicyCode,
                        PolicyName = group.Key.PolicyNameSnapshot,
                        TotalRequests = policyRefunds.Count,
                        ApprovedRequests = approvedRequests,
                        RejectedRequests = policyRefunds.Count(refund => refund.Status == RefundRequestStatus.Rejected),
                        PendingRequests = policyRefunds.Count(refund => refund.Status == RefundRequestStatus.Pending),
                        TotalAmount = policyRefunds.Sum(refund => refund.RefundAmount),
                        ApprovedAmount = policyRefunds
                            .Where(refund => refund.Status == RefundRequestStatus.Approved)
                            .Sum(refund => refund.RefundAmount),
                        ApprovalRate = CalculateRate(approvedRequests, policyRefunds.Count)
                    };
                })
                .OrderByDescending(item => item.TotalRequests)
                .ThenByDescending(item => item.TotalAmount)
                .ToList();

            return Result.Success(
                new GetRefundPoliciesReportResponse
                {
                    Range = ReportDateRange.ToResponse(range),
                    Items = items
                },
                "Refund policies report retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving refund policies report");
            return Result.Failure<GetRefundPoliciesReportResponse>(
                Error.ServerError,
                "An error occurred while retrieving refund policies report.");
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
