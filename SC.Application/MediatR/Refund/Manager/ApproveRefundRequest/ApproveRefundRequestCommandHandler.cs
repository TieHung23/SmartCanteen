using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SC.Domain.Domain.WalletTransaction.Enum;
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using UserAggregate = SC.Domain.Domain.User.User;
using WalletTransactionEntity = SC.Domain.Domain.WalletTransaction.Entity.WalletTransaction;

namespace SC.Application.MediatR.Refund.Manager.ApproveRefundRequest;

internal sealed class ApproveRefundRequestCommandHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<UserAggregate, Guid> userRepository,
    IGenericRepository<WalletTransactionEntity, Guid> walletTransactionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IWalletDomainService walletDomainService,
    IBusinessNotificationService businessNotificationService,
    ILogger<ApproveRefundRequestCommandHandler> logger)
    : ICommandHandler<ApproveRefundRequestCommand, ApproveRefundRequestResponse>
{
    /// <summary>
    /// Whether approving a full-order refund should also close the order out. Only orders still
    /// on their way to the customer are cancelled: a Completed order's food was already collected,
    /// so cancelling it would rewrite history (and a later rejection would revive it as Preparing),
    /// while Cancelled and Expired orders are already closed.
    /// </summary>
    private static bool CanBeCancelledByRefund(OrderStatus status)
        => status is OrderStatus.Pending or OrderStatus.Preparing or OrderStatus.ReadyForPickup;

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
            var creditResult = await walletDomainService.TryCreditUserBalanceAsync(
                user.Id,
                refund.RefundAmount,
                cancellationToken);

            if (creditResult.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<ApproveRefundRequestResponse>(
                    creditResult.Error ?? Error.ServerError,
                    creditResult.Message);
            }

            var balanceAfter = creditResult.Value;

            var walletTransaction = WalletTransactionEntity.Create(
                user.Id,
                refund.RefundAmount,
                balanceBefore,
                balanceAfter,
                WalletTransactionType.Refund);

            refund.Approve(currentUserService.UserId, walletTransaction.Id);

            var order = await orderRepository.FindSingleAsync(
                order => !order.IsDeleted && order.Id == refund.OrderId,
                query => query.Include(order => order.OrderItems),
                cancellationToken);

            if (refund.OrderItemId.HasValue)
            {
                var item = order?.OrderItems.FirstOrDefault(item => item.Id == refund.OrderItemId.Value);
                if (item is null)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure<ApproveRefundRequestResponse>(
                        Error.NullValue,
                        "Refund request order item not found.");
                }

                item.CompleteRefund();
                orderRepository.Update(order!);
            }
            else if (order is not null && CanBeCancelledByRefund(order.Status))
            {
                // Refunding a whole order that is still being handled has to close it out too,
                // otherwise the robot keeps assembling it and the customer collects food they have
                // already been paid back for. Matches how the change-proposal and auto-finalize
                // refund flows close their orders.
                var fromStatus = order.Status;
                order.UpdateStatus(OrderStatus.Cancelled, currentUserService.UserId);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(
                        order.Id,
                        fromStatus,
                        OrderStatus.Cancelled,
                        currentUserService.UserId,
                        "RefundApproved",
                        $"Full order refund request {refund.Id} was approved."),
                    cancellationToken);
                orderRepository.Update(order);
            }

            await walletTransactionRepository.AddAsync(
                walletTransaction,
                cancellationToken);
            refundRepository.Update(refund);

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
