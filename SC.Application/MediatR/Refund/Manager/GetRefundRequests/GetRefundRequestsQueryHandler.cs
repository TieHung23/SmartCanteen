using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using UserAggregateRoot = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequests;

internal sealed class GetRefundRequestsQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregateRoot, Guid> userRepository,
    ILogger<GetRefundRequestsQueryHandler> logger)
    : IQueryHandler<GetRefundRequestsQuery, PaginatedList<GetRefundRequestsResponse>>
{
    public async Task<Result<PaginatedList<GetRefundRequestsResponse>>> Handle(
        GetRefundRequestsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var allRequests = await refundRepository
                .FindListAsync(refund => !refund.IsDeleted, cancellationToken,
                    refund => refund.Images);
            var filtered = allRequests.AsEnumerable();

            if (request.Status.HasValue)
            {
                if (!Enum.IsDefined(typeof(RefundRequestStatus), request.Status.Value))
                {
                    return Result.Failure<PaginatedList<GetRefundRequestsResponse>>(
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

            var userIds = requests.Select(r => r.UserId).Distinct().ToList();
            var users = await userRepository
                .FindListAsync(u => userIds.Contains(u.Id), cancellationToken);
            var userMap = users.ToDictionary(u => u.Id);

            var responses = requests.Select(refund =>
            {
                var user = userMap.GetValueOrDefault(refund.UserId);
                return new GetRefundRequestsResponse
                {
                    Id = refund.Id,
                    OrderId = refund.OrderId,
                    OrderItemId = refund.OrderItemId,
                    ChangeProposalId = refund.ChangeProposalId,
                    DishId = refund.DishId,
                    UserId = refund.UserId,
                    UserName = user?.Name ?? string.Empty,
                    StudentId = user?.StudentId,
                    PolicyName = refund.PolicyNameSnapshot,
                    RefundPercent = refund.RefundPercentSnapshot,
                    OrderAmount = refund.OrderAmountSnapshot,
                    RefundAmount = refund.RefundAmount,
                    Status = refund.Status.ToString(),
                    ImageCount = refund.Images.Count(image => !image.IsDeleted),
                    CreatedAtUtc = refund.CreatedAtUtc,
                    ReviewedAtUtc = refund.ReviewedAtUtc
                };
            }).ToList();

            return Result.Success(
                new PaginatedList<GetRefundRequestsResponse>(
                    responses,
                    request.PageNumber,
                    request.PageSize,
                    totalCount),
                "Refund requests retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving refund requests for manager");
            return Result.Failure<PaginatedList<GetRefundRequestsResponse>>(
                Error.ServerError,
                "An error occurred while retrieving refund requests.");
        }
    }
}
