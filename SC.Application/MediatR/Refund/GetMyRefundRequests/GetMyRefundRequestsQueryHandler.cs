using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Refund.GetMyRefundRequests;

internal sealed class GetMyRefundRequestsQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
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
            var proposalIds = requests
                .Where(refund => refund.ChangeProposalId.HasValue)
                .Select(refund => refund.ChangeProposalId!.Value)
                .Distinct()
                .ToList();
            List<OrderItemChangeProposal> proposals = proposalIds.Count == 0
                ? []
                : await proposalRepository.FindListAsync(
                    proposal => proposalIds.Contains(proposal.Id),
                    cancellationToken);
            var proposalMap = proposals.ToDictionary(proposal => proposal.Id);
            var dishIds = requests
                .Select(refund => refund.DishId)
                .Concat(proposals.Select(proposal => (Guid?)proposal.CurrentDishId))
                .Concat(proposals.Select(proposal => proposal.SuggestedDishId))
                .Concat(proposals.Select(proposal => proposal.SelectedDishId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            List<DishAggregateRoot> dishes = dishIds.Count == 0
                ? []
                : await dishRepository.FindListAsync(
                    dish => dishIds.Contains(dish.Id),
                    cancellationToken);
            var dishMap = dishes.ToDictionary(dish => dish.Id);

            var responses = requests.Select(refund =>
            {
                var proposal = refund.ChangeProposalId.HasValue
                    ? proposalMap.GetValueOrDefault(refund.ChangeProposalId.Value)
                    : null;

                return new GetMyRefundRequestsResponse
                {
                    Id = refund.Id,
                    OrderId = refund.OrderId,
                    OrderItemId = refund.OrderItemId,
                    ChangeProposalId = refund.ChangeProposalId,
                    DishId = refund.DishId,
                    DishName = GetDishName(refund.DishId, dishMap),
                    CurrentDishId = proposal?.CurrentDishId,
                    CurrentDishName = GetDishName(proposal?.CurrentDishId, dishMap),
                    SuggestedDishId = proposal?.SuggestedDishId,
                    SuggestedDishName = GetDishName(proposal?.SuggestedDishId, dishMap),
                    SelectedDishId = proposal?.SelectedDishId,
                    SelectedDishName = GetDishName(proposal?.SelectedDishId, dishMap),
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

    private static string? GetDishName(
        Guid? dishId,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishes)
    {
        if (!dishId.HasValue || !dishes.TryGetValue(dishId.Value, out var dish))
        {
            return null;
        }

        return dish.Name;
    }
}
