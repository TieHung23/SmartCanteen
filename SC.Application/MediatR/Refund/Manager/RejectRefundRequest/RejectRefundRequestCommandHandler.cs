using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;

namespace SC.Application.MediatR.Refund.Manager.RejectRefundRequest;

internal sealed class RejectRefundRequestCommandHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IBusinessNotificationService businessNotificationService,
    ILogger<RejectRefundRequestCommandHandler> logger)
    : ICommandHandler<RejectRefundRequestCommand, RejectRefundRequestResponse>
{
    public async Task<Result<RejectRefundRequestResponse>> Handle(
        RejectRefundRequestCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await refundLockService.LockRefundRequestAsync(request.Id, cancellationToken);

            var refund = await refundRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            if (refund is null || refund.IsDeleted)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<RejectRefundRequestResponse>(
                    Error.NullValue,
                    "Refund request not found.");
            }

            if (refund.Status != RefundRequestStatus.Pending)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<RejectRefundRequestResponse>(
                    Error.InvalidValue,
                    "Refund request is no longer pending.");
            }

            refund.Reject(currentUserService.UserId, request.Reason);
            refundRepository.Update(refund);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.RefundRejected,
                refund.UserId,
                refund.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = refund.Id.ToString(),
                    ["reason"] = refund.RejectionReason!
                },
                new
                {
                    RefundRequestId = refund.Id,
                    refund.OrderId,
                    RejectionReason = refund.RejectionReason
                },
                cancellationToken);

            return Result.Success(
                new RejectRefundRequestResponse
                {
                    Id = refund.Id,
                    Status = refund.Status.ToString(),
                    RejectionReason = refund.RejectionReason!
                },
                "Refund request rejected successfully.");
        }
        catch (InvalidOperationException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<RejectRefundRequestResponse>(
                Error.InvalidValue,
                ex.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(
                ex,
                "Error rejecting refund request {RefundRequestId}",
                request.Id);
            return Result.Failure<RejectRefundRequestResponse>(
                Error.ServerError,
                "An error occurred while rejecting the refund request.");
        }
    }
}
