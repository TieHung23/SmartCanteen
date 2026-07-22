using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;

namespace SC.Application.MediatR.Refund.GetMyRefundRequests;

internal sealed class GetMyRefundRequestsQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    ICurrentUserService currentUserService,
    ILogger<GetMyRefundRequestsQueryHandler> logger)
    : IQueryHandler<GetMyRefundRequestsQuery, PaginatedList<GetMyRefundRequestsResponse>>
{
    public async Task<Result<PaginatedList<GetMyRefundRequestsResponse>>> Handle(
        GetMyRefundRequestsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var allRequests = await refundRepository
                .FindListAsync(refund =>
                    !refund.IsDeleted
                    && refund.UserId == userId,
                    cancellationToken,
                    refund => refund.Images);
            var filtered = allRequests.AsEnumerable();

            if (request.Status.HasValue)
            {
                if (!Enum.IsDefined(typeof(RefundRequestStatus), request.Status.Value))
                {
                    return Result.Failure<PaginatedList<GetMyRefundRequestsResponse>>(
                        Error.InvalidValue,
                        "Refund request status is invalid.");
                }

                filtered = filtered.Where(
                    refund => (int)refund.Status == request.Status.Value);
            }

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var requests = filteredList
                .OrderByDescending(refund => refund.CreatedAtUtc)
                .Skip(request.GetSkipCount())
                .Take(request.PageSize)
                .ToList();

            var responses = requests.Select(refund => new GetMyRefundRequestsResponse
            {
                Id = refund.Id,
                OrderId = refund.OrderId,
                OrderItemId = refund.OrderItemId,
                ChangeProposalId = refund.ChangeProposalId,
                DishId = refund.DishId,
                PolicyName = refund.PolicyNameSnapshot,
                RefundPercent = refund.RefundPercentSnapshot,
                OrderAmount = refund.OrderAmountSnapshot,
                RefundAmount = refund.RefundAmount,
                Status = refund.Status.ToString(),
                ImageCount = refund.Images.Count(image => !image.IsDeleted),
                CreatedAtUtc = refund.CreatedAtUtc,
                ReviewedAtUtc = refund.ReviewedAtUtc
            }).ToList();

            return Result.Success(
                new PaginatedList<GetMyRefundRequestsResponse>(
                    responses,
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Refund requests retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving refund requests for current user");
            return Result.Failure<PaginatedList<GetMyRefundRequestsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving refund requests.");
        }
    }
}
