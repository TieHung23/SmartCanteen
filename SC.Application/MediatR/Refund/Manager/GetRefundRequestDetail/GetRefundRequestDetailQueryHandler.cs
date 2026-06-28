using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Refund.AggregateRoot;
using UserAggregateRoot = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Refund.Manager.GetRefundRequestDetail;

internal sealed class GetRefundRequestDetailQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregateRoot, Guid> userRepository,
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

            var response = new GetRefundRequestDetailResponse
            {
                Id = refund.Id,
                OrderId = refund.OrderId,
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
}
