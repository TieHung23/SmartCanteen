using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Refund.AggregateRoot;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using UserAggregateRoot = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequestDetail;

internal sealed class GetRefundRequestDetailQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregateRoot, Guid> userRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetRefundRequestDetailQueryHandler> logger)
    : IQueryHandler<GetRefundRequestDetailQuery, GetRefundRequestDetailResponse>
{
    public async Task<Result<GetRefundRequestDetailResponse>> Handle(
        GetRefundRequestDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var refund = await refundRepository
                .FindSingleAsync(refund =>
                    !refund.IsDeleted
                    && refund.Id == request.Id,
                    cancellationToken,
                    refund => refund.Images);

            if (refund is null)
            {
                return Result.Failure<GetRefundRequestDetailResponse>(
                    Error.NullValue,
                    "Refund request not found.");
            }

            var user = await userRepository.GetByIdAsync(refund.UserId, cancellationToken);
            var proposal = refund.ChangeProposalId.HasValue
                ? await proposalRepository.GetByIdAsync(refund.ChangeProposalId.Value, cancellationToken)
                : null;
            var dishIds = new[]
                {
                    refund.DishId,
                    proposal?.CurrentDishId,
                    proposal?.SuggestedDishId,
                    proposal?.SelectedDishId
                }
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

            var response = new GetRefundRequestDetailResponse
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
                UserEmail = user?.Email ?? string.Empty,
                StudentId = user?.StudentId,
                PolicyCode = refund.PolicyCode,
                PolicyName = refund.PolicyNameSnapshot,
                RefundPercent = refund.RefundPercentSnapshot,
                OrderAmount = refund.OrderAmountSnapshot,
                RefundAmount = refund.RefundAmount,
                Description = refund.Description,
                Status = refund.Status.ToString(),
                Images = refund.Images
                    .Where(image => !image.IsDeleted)
                    .Select(image => new ManagerRefundImageResponse
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl,
                        FileName = image.FileName
                    })
                    .ToList(),
                ReviewedBy = refund.ReviewedBy,
                ReviewedAtUtc = refund.ReviewedAtUtc,
                RejectionReason = refund.RejectionReason,
                WalletTransactionId = refund.WalletTransactionId,
                CreatedAtUtc = refund.CreatedAtUtc
            };

            return Result.Success(
                response,
                "Refund request retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving refund request {RefundRequestId} for manager",
                request.Id);
            return Result.Failure<GetRefundRequestDetailResponse>(
                Error.ServerError,
                "An error occurred while retrieving the refund request.");
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
