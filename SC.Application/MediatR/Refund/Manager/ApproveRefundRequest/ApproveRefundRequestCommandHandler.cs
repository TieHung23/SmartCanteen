using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SC.Domain.Domain.WalletTransaction.Entity;
using SC.Domain.Domain.WalletTransaction.Enum;
using SC.Domain.SharedKernel.ValueObjects;
using UserAggregate = SC.Domain.Domain.User.User;

namespace SC.Application.MediatR.Refund.Manager.ApproveRefundRequest;

internal sealed class ApproveRefundRequestCommandHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<WalletTransaction, Guid> walletTransactionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IWalletDomainService walletDomainService,
    IBusinessNotificationService businessNotificationService,
    ILogger<ApproveRefundRequestCommandHandler> logger)
    : ICommandHandler<ApproveRefundRequestCommand, ApproveRefundRequestResponse>
{
    public async Task<Result<ApproveRefundRequestResponse>> Handle(
        ApproveRefundRequestCommand request,
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
                return Result.Failure<ApproveRefundRequestResponse>(
                    Error.NullValue,
                    "Refund request not found.");
            }

            if (refund.Status != RefundRequestStatus.Pending)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveRefundRequestResponse>(
                    Error.InvalidValue,
                    "Refund request is no longer pending.");
            }

            await walletDomainService.LockUserAsync(refund.UserId, cancellationToken);
            var user = await userRepository.GetByIdAsync(
                refund.UserId,
                cancellationToken);

            if (user is null || user.IsDeleted)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveRefundRequestResponse>(
                    Error.NullValue,
                    "Refund request user not found.");
            }

            var balanceBefore = user.Balance.Amount;
            var balanceAfter = balanceBefore + refund.RefundAmount;
            user.UpdateBalance(Money.Create(balanceAfter, user.Balance.Currency));

            var walletTransaction = WalletTransaction.Create(
                user.Id,
                refund.RefundAmount,
                balanceBefore,
                balanceAfter,
                WalletTransactionType.Refund);

            refund.Approve(currentUserService.UserId, walletTransaction.Id);

            await walletTransactionRepository.AddAsync(
                walletTransaction,
                cancellationToken);
            refundRepository.Update(refund);
            userRepository.Update(user);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.RefundApproved,
                refund.UserId,
                refund.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = refund.Id.ToString(),
                    ["refundAmount"] = refund.RefundAmount.ToString("0.##")
                },
                new
                {
                    RefundRequestId = refund.Id,
                    refund.OrderId,
                    refund.RefundAmount,
                    BalanceAfter = balanceAfter,
                    WalletTransactionId = walletTransaction.Id
                },
                cancellationToken);

            return Result.Success(
                new ApproveRefundRequestResponse
                {
                    Id = refund.Id,
                    WalletTransactionId = walletTransaction.Id,
                    RefundAmount = refund.RefundAmount,
                    BalanceAfter = balanceAfter,
                    Status = refund.Status.ToString()
                },
                "Refund request approved and wallet credited successfully.");
        }
        catch (InvalidOperationException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<ApproveRefundRequestResponse>(
                Error.InvalidValue,
                ex.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(
                ex,
                "Error approving refund request {RefundRequestId}",
                request.Id);
            return Result.Failure<ApproveRefundRequestResponse>(
                Error.ServerError,
                "An error occurred while approving the refund request.");
        }
    }
}
