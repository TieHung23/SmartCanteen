using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using UserAggregateRoot = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequests;

internal sealed class GetRefundRequestsQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregateRoot, Guid> userRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
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
                var user = userMap.GetValueOrDefault(refund.UserId);
                var proposal = refund.ChangeProposalId.HasValue
                    ? proposalMap.GetValueOrDefault(refund.ChangeProposalId.Value)
                    : null;
                return new GetRefundRequestsResponse
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
