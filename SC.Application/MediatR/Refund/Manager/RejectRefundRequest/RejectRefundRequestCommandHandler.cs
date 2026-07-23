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
using OrderAggregate = SC.Domain.Domain.Order.AggregateRoot.Order;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Refund.Manager.RejectRefundRequest;

internal sealed class RejectRefundRequestCommandHandler(
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<OrderAggregate, Guid> orderRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
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

            OrderAggregate? order = null;
            if (refund.OrderItemId.HasValue)
            {
                order = await orderRepository.FindSingleAsync(
                    order => !order.IsDeleted && order.Id == refund.OrderId,
                    query => query.Include(order => order.OrderItems),
                    cancellationToken);

                var item = order?.OrderItems.FirstOrDefault(item => item.Id == refund.OrderItemId.Value);
                if (item is null)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure<RejectRefundRequestResponse>(
                        Error.NullValue,
                        "Refund request order item not found.");
                }

                item.CancelRefund();
                orderRepository.Update(order!);
            }
            else if (refund.ChangeProposalId.HasValue)
            {
                order = await orderRepository.FindSingleAsync(
                    order => !order.IsDeleted && order.Id == refund.OrderId,
                    cancellationToken);

                if (order is null)
                {
                    await unitOfWork.RollbackAsync(cancellationToken);
                    return Result.Failure<RejectRefundRequestResponse>(
                        Error.NullValue,
                        "Refund request order not found.");
                }

                var fromStatus = order.Status;
                if (fromStatus == OrderStatus.Cancelled)
                {
                    order.UpdateStatus(OrderStatus.Preparing, currentUserService.UserId);
                    await orderStatusHistoryRepository.AddAsync(
                        OrderStatusHistoryEntity.Create(
                            order.Id,
                            fromStatus,
                            OrderStatus.Preparing,
                            currentUserService.UserId,
                            "ChangeProposalOrderRefundRejected"),
                        cancellationToken);
                }

                var proposals = await proposalRepository.FindListAsync(
                    proposal =>
                        proposal.OrderId == refund.OrderId
                        && proposal.ProposalStatus == ChangeProposalStatus.OrderRefundRequested,
                    cancellationToken);

                foreach (var proposal in proposals)
                {
                    proposal.ReopenOrderRefundRequest(currentUserService.UserId);
                    proposalRepository.Update(proposal);
                }

                orderRepository.Update(order);
            }

            if (refund.OrderItemId.HasValue && refund.ChangeProposalId.HasValue)
            {
                var proposal = await proposalRepository.GetByIdAsync(
                    refund.ChangeProposalId.Value,
                    cancellationToken);

                if (proposal is not null)
                {
                    proposal.ReopenRefundRequest(currentUserService.UserId);
                    proposalRepository.Update(proposal);
                }
            }

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
