using Microsoft.Extensions.Logging;
using SC.Application.MediatR.RefundPolicy;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Session.DeleteSession;

internal class DeleteSessionCommandHandler(
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IRefundAutoCreditService refundAutoCreditService,
    IBusinessNotificationService businessNotificationService,
    ILogger<DeleteSessionCommandHandler> logger
) : ICommandHandler<DeleteSessionCommand, DeleteSessionResponse>
{
    public async Task<Result<DeleteSessionResponse>> Handle(
        DeleteSessionCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(request.Id, cancellationToken);
            if (session is null || session.IsDeleted)
            {
                return Result.Failure<DeleteSessionResponse>(
                    Error.NullValue,
                    $"Session with id {request.Id} not found.");
            }

            // Orders that are already cancelled, expired or collected are settled - deleting the
            // session changes nothing for them.
            var orders = await orderRepository.FindListAsync(
                o => o.SessionId == session.Id
                     && !o.IsDeleted
                     && o.Status != OrderStatus.Cancelled
                     && o.Status != OrderStatus.Expired
                     && o.Status != OrderStatus.Completed,
                cancellationToken);

            var orderIds = orders.Select(o => o.Id).ToList();

            var servingJobs = orderIds.Count == 0
                ? []
                : await servingJobRepository.FindListAsync(
                    j => orderIds.Contains(j.OrderId) && !j.IsDeleted,
                    cancellationToken);

            var jobsByOrder = servingJobs
                .GroupBy(j => j.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // A session with nothing left to settle must still be deletable even on an instance
            // where the refund policy was never configured, so the policy is only demanded once
            // there is an order that actually needs refunding.
            var refundable = orders
                .Where(o => !HasStartedServing(jobsByOrder.GetValueOrDefault(o.Id, [])))
                .ToList();

            RefundPolicyDefinition? policy = null;
            if (refundable.Count > 0)
            {
                var policyResult = await GetConfiguredPolicyAsync(cancellationToken);
                if (policyResult.IsFailure)
                    return Result.Failure<DeleteSessionResponse>(policyResult.Error!, policyResult.Message);

                policy = policyResult.Value!;
                if (policy.RequiresImage)
                {
                    return Result.Failure<DeleteSessionResponse>(
                        Error.InvalidValue,
                        "Configured session deletion refund policy cannot require images.");
                }
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var pendingNotifications = new List<RefundNotification>();
            var refundedCount = 0;
            var skippedCount = orders.Count - refundable.Count;

            foreach (var order in refundable)
            {
                await refundLockService.LockOrderRefundRequestsAsync(order.Id, cancellationToken);

                // Someone already has a full-order refund in flight for this order (the customer
                // answered a change proposal, or the expiration job got there first). Leave it to
                // that flow rather than crediting the same order twice.
                var activeOrderRefundExists = await refundRepository.ExistsAsync(
                    refund =>
                        !refund.IsDeleted
                        && refund.OrderId == order.Id
                        && !refund.OrderItemId.HasValue
                        && (refund.Status == RefundRequestStatus.Pending
                            || refund.Status == RefundRequestStatus.Approved),
                    cancellationToken);

                if (activeOrderRefundExists)
                {
                    skippedCount++;
                    continue;
                }

                var orderAmount = order.OrderItems
                    .Where(item => item.ItemStatus != OrderItemStatus.Refunded)
                    .Sum(item => item.UnitPrice.Amount * item.Quantity);

                // Every line was already refunded individually. There is nothing left to give back,
                // but the order still has to be closed out so no robot picks it up later.
                if (orderAmount > 0)
                {
                    var refundRequest = RefundRequest.Submit(
                        order.Id,
                        order.CreatedBy,
                        policy!.Scope,
                        policy.Name,
                        policy.Percent,
                        orderAmount,
                        $"Full order refund because session {session.Id} was deleted.");

                    await refundRepository.AddAsync(refundRequest, cancellationToken);

                    var creditResult = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
                    if (creditResult.IsFailure)
                    {
                        await unitOfWork.RollbackAsync(cancellationToken);
                        return Result.Failure<DeleteSessionResponse>(
                            creditResult.Error ?? Error.ServerError,
                            creditResult.Message);
                    }

                    var credit = creditResult.Value!;
                    pendingNotifications.Add(new RefundNotification(
                        order.CreatedBy,
                        refundRequest.Id,
                        order.Id,
                        refundRequest.RefundAmount,
                        credit.WalletTransactionId,
                        credit.BalanceAfter,
                        refundRequest.Status.ToString()));

                    refundedCount++;
                }

                await CancelOrderAsync(order, cancellationToken);
                await CancelServingJobsAsync(jobsByOrder.GetValueOrDefault(order.Id, []), cancellationToken);
                await CloseOpenProposalsAsync(order.Id, cancellationToken);

                orderRepository.Update(order);
            }

            session.SoftDelete();
            sessionRepository.Update(session);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            // Only after the money and the cancellations are durably committed.
            foreach (var notification in pendingNotifications)
                await NotifyCustomerAsync(notification, cancellationToken);

            var response = new DeleteSessionResponse
            {
                Id = request.Id,
                RefundedOrderCount = refundedCount,
                SkippedOrderCount = skippedCount,
                Message = refundedCount == 0
                    ? "Session deleted successfully (soft delete)."
                    : $"Session deleted successfully (soft delete). {refundedCount} order(s) were cancelled and refunded."
            };

            return Result.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting session with id {SessionId}", request.Id);
            return Result.Failure<DeleteSessionResponse>(
                Error.ServerError,
                "An error occurred while deleting the session.");
        }
    }

    /// <summary>
    /// True once the robot has taken the order past the queue. Same boundary the customer-facing
    /// full-order refund enforces (RequestOrderRefundFromProposalCommandHandler): only a job still
    /// sitting in the queue - or no job at all - may be pulled back. Anything Pushed or beyond is
    /// food already on its way to a tray, so the order is left alone and the customer still collects
    /// it even though the session is gone.
    /// </summary>
    private static bool HasStartedServing(IReadOnlyCollection<ServingJobEntity> jobs)
        => jobs.Any(j => j.Status != ServingJobStatus.Queued && j.Status != ServingJobStatus.Cancelled);

    private async Task CancelOrderAsync(OrderAggregateRoot order, CancellationToken cancellationToken)
    {
        var fromStatus = order.Status;
        if (fromStatus == OrderStatus.Cancelled)
            return;

        order.UpdateStatus(OrderStatus.Cancelled, currentUserService.UserId);
        await orderStatusHistoryRepository.AddAsync(
            OrderStatusHistoryEntity.Create(
                order.Id,
                fromStatus,
                OrderStatus.Cancelled,
                currentUserService.UserId,
                "SessionDeleted"),
            cancellationToken);
    }

    // A queued job holds a tray only after a requeue; that tray goes back to the pool so the next
    // session can use it, and the job is cancelled in the same transaction as the refund so no
    // robot can pull an order that was just paid back.
    private async Task CancelServingJobsAsync(
        IReadOnlyCollection<ServingJobEntity> jobs,
        CancellationToken cancellationToken)
    {
        foreach (var job in jobs.Where(j => j.Status == ServingJobStatus.Queued))
        {
            if (job.TrayId is Guid heldTrayId)
            {
                var heldTray = await trayRepository.GetByIdAsync(heldTrayId, cancellationToken);
                if (heldTray is not null)
                {
                    heldTray.Release(currentUserService.UserId);
                    trayRepository.Update(heldTray);
                }
                job.ClearTray(currentUserService.UserId);
            }

            job.Cancel(currentUserService.UserId);
            servingJobRepository.Update(job);
        }
    }

    /// <summary>
    /// A proposal left WaitingResponse on a deleted session can never be answered, and the
    /// expiration job would otherwise try to refund an order that was just refunded here.
    /// </summary>
    private async Task CloseOpenProposalsAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var openProposals = await proposalRepository.FindListAsync(
            p => p.OrderId == orderId && p.ProposalStatus == ChangeProposalStatus.WaitingResponse,
            cancellationToken);

        foreach (var proposal in openProposals)
        {
            proposal.RequestOrderRefund(currentUserService.UserId);
            proposalRepository.Update(proposal);
        }
    }

    private async Task<Result<RefundPolicyDefinition>> GetConfiguredPolicyAsync(
        CancellationToken cancellationToken)
    {
        var policyCodeSetting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == SessionRefundSettingConstants.Group
                && setting.Scope.ToUpper() == SessionRefundSettingConstants.RefundScope
                && setting.Code.ToUpper() == SessionRefundSettingConstants.DeleteRefundPolicyCode,
            cancellationToken);

        if (policyCodeSetting is null || string.IsNullOrWhiteSpace(policyCodeSetting.Value))
        {
            return Result.Failure<RefundPolicyDefinition>(
                Error.InvalidValue,
                "Session deletion refund policy is not configured.");
        }

        var policyCode = policyCodeSetting.Value.Trim();
        var policySettings = await settingRepository.FindListAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == RefundPolicyConstants.Group
                && setting.Scope.ToLower() == policyCode.ToLower(),
            cancellationToken);

        if (!RefundPolicyDefinition.TryCreate(policyCode, policySettings, out var policy))
        {
            return Result.Failure<RefundPolicyDefinition>(
                Error.InvalidValue,
                "Configured session deletion refund policy is not active or invalid.");
        }

        return Result.Success(policy!, "Refund policy retrieved successfully.");
    }

    private async Task NotifyCustomerAsync(RefundNotification notification, CancellationToken cancellationToken)
    {
        await businessNotificationService.NotifyAsync(
            NotificationTemplateKeys.RefundApproved,
            notification.UserId,
            notification.RefundRequestId,
            new Dictionary<string, string>
            {
                ["referenceId"] = notification.RefundRequestId.ToString(),
                ["refundAmount"] = notification.RefundAmount.ToString("0.##")
            },
            new
            {
                RefundRequestId = notification.RefundRequestId,
                OrderId = notification.OrderId,
                RefundAmount = notification.RefundAmount,
                WalletTransactionId = notification.WalletTransactionId,
                BalanceAfter = notification.BalanceAfter,
                Status = notification.Status,
                Reason = "SessionDeleted"
            },
            cancellationToken);
    }

    private sealed record RefundNotification(
        Guid UserId,
        Guid RefundRequestId,
        Guid OrderId,
        decimal RefundAmount,
        Guid WalletTransactionId,
        decimal BalanceAfter,
        string Status);
}
