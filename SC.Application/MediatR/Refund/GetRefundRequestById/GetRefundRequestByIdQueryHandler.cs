using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Refund.AggregateRoot;

namespace SC.Application.MediatR.Refund.GetRefundRequestById;

internal sealed class GetRefundRequestByIdQueryHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    ICurrentUserService currentUserService,
    ILogger<GetRefundRequestByIdQueryHandler> logger)
    : IQueryHandler<GetRefundRequestByIdQuery, GetRefundRequestByIdResponse>
{
    public async Task<Result<GetRefundRequestByIdResponse>> Handle(
        GetRefundRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = currentUserService.UserId;
            var refund = await refundRepository
                .FindSingleAsync(refund =>
                    !refund.IsDeleted
                    && refund.Id == request.Id
                    && refund.UserId == userId,
                    cancellationToken,
                    refund => refund.Images);

            if (refund is null)
            {
                return Result.Failure<GetRefundRequestByIdResponse>(
                    Error.NullValue,
                    "Refund request not found.");
            }

            var response = new GetRefundRequestByIdResponse
            {
                Id = refund.Id,
                OrderId = refund.OrderId,
                PolicyCode = refund.PolicyCode,
                PolicyName = refund.PolicyNameSnapshot,
                RefundPercent = refund.RefundPercentSnapshot,
                OrderAmount = refund.OrderAmountSnapshot,
                RefundAmount = refund.RefundAmount,
                Description = refund.Description,
                Status = refund.Status.ToString(),
                Images = refund.Images
                    .Where(image => !image.IsDeleted)
                    .Select(image => new RefundImageResponse
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

            return Result.Success(response, "Refund request retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error retrieving refund request {RefundRequestId}",
                request.Id);
            return Result.Failure<GetRefundRequestByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving the refund request.");
        }
    }
}
