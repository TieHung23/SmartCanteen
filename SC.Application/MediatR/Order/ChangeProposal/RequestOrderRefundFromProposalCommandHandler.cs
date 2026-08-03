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
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class RequestOrderRefundFromProposalCommandHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IRefundAutoCreditService refundAutoCreditService,
    IBusinessNotificationService businessNotificationService,
    ILogger<RequestOrderRefundFromProposalCommandHandler> logger)
    : ICommandHandler<RequestOrderRefundFromProposalCommand, RequestOrderRefundFromProposalResponse>
{
    public async Task<Result<RequestOrderRefundFromProposalResponse>> Handle(
        RequestOrderRefundFromProposalCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var proposal = await proposalRepository.FindSingleAsync(
                p => p.Id == request.ProposalId,
                cancellationToken);

            if (proposal is null)
                return Result.Failure<RequestOrderRefundFromProposalResponse>(Error.NullValue, "Proposal not found.");

            if (proposal.UserId != currentUserService.UserId)
                return Result.Failure<RequestOrderRefundFromProposalResponse>(Error.InvalidValue, "This proposal does not belong to you.");

            if (proposal.IsExpired(DateTimeOffset.UtcNow))
            {
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    Error.InvalidValue,
                    "Change proposal has expired.");
            }

            var order = await orderRepository.FindSingleAsync(
                o => o.Id == proposal.OrderId && !o.IsDeleted,
                cancellationToken);

            if (order is null)
                return Result.Failure<RequestOrderRefundFromProposalResponse>(Error.NullValue, "Order not found.");

            if (order.Status != OrderStatus.Preparing)
            {
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    Error.InvalidValue,
                    "Order is no longer available for change proposal actions.");
            }

            var session = await sessionRepository.FindSingleAsync(
                s => s.Id == order.SessionId && !s.IsDeleted,
                cancellationToken);

            if (session is null)
                return Result.Failure<RequestOrderRefundFromProposalResponse>(Error.NullValue, "Session not found.");

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            await refundLockService.LockOrderRefundRequestsAsync(order.Id, cancellationToken);

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
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    Error.InvalidValue,
                    "This order already has a pending or approved full order refund request.");
            }

            var policyResult = await GetConfiguredPolicyAsync(cancellationToken);
            if (policyResult.IsFailure)
                return Result.Failure<RequestOrderRefundFromProposalResponse>(policyResult.Error!, policyResult.Message);

            var policy = policyResult.Value!;
            if (policy.RequiresImage)
            {
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    Error.InvalidValue,
                    "Configured change proposal order refund policy cannot require images.");
            }

            var orderAmount = order.OrderItems
                .Where(item => item.ItemStatus != OrderItemStatus.Refunded)
                .Sum(item => item.UnitPrice.Amount * item.Quantity);
            if (orderAmount <= 0)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    Error.InvalidValue,
                    "Remaining order amount must be greater than zero.");
            }

            var refundRequest = RefundRequest.Submit(
                order.Id,
                currentUserService.UserId,
                policy.Scope,
                policy.Name,
                policy.Percent,
                orderAmount,
                $"Full order refund requested from change proposal {proposal.Id}.");
            refundRequest.AttachProposalContext(
                orderItemId: null,
                changeProposalId: proposal.Id,
                dishId: proposal.CurrentDishId);

            proposal.RequestOrderRefund(currentUserService.UserId);

            var siblingProposals = await proposalRepository.FindListAsync(
                sibling =>
                    sibling.OrderId == order.Id
                    && sibling.Id != proposal.Id
                    && sibling.ProposalStatus == ChangeProposalStatus.WaitingResponse,
                cancellationToken);

            foreach (var sibling in siblingProposals)
            {
                sibling.RequestOrderRefund(currentUserService.UserId);
                proposalRepository.Update(sibling);
            }

            var fromStatus = order.Status;
            if (fromStatus != OrderStatus.Cancelled)
            {
                order.UpdateStatus(OrderStatus.Cancelled, currentUserService.UserId);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(
                        order.Id,
                        fromStatus,
                        OrderStatus.Cancelled,
                        currentUserService.UserId,
                        "ChangeProposalOrderRefund"),
                    cancellationToken);
            }

            await refundRepository.AddAsync(refundRequest, cancellationToken);

            var creditResult = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
            if (creditResult.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<RequestOrderRefundFromProposalResponse>(
                    creditResult.Error ?? Error.ServerError,
                    creditResult.Message);
            }

            var credit = creditResult.Value!;

            proposalRepository.Update(proposal);
            orderRepository.Update(order);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.RefundApproved,
                currentUserService.UserId,
                refundRequest.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = refundRequest.Id.ToString(),
                    ["refundAmount"] = refundRequest.RefundAmount.ToString("0.##")
                },
                new
                {
                    RefundRequestId = refundRequest.Id,
                    refundRequest.OrderId,
                    refundRequest.RefundAmount,
                    ProposalId = proposal.Id,
                    BalanceAfter = credit.BalanceAfter,
                    WalletTransactionId = credit.WalletTransactionId,
                    Status = refundRequest.Status.ToString()
                },
                cancellationToken);

            await NotifyManagerAsync(
                session.CreatedBy,
                order.Id,
                proposal.Id,
                currentUserService.UserId,
                refundRequest.Id,
                refundRequest.RefundAmount,
                cancellationToken);

            var response = new RequestOrderRefundFromProposalResponse
            {
                RefundRequestId = refundRequest.Id,
                OrderId = refundRequest.OrderId,
                PolicyCode = refundRequest.PolicyCode,
                RefundAmount = refundRequest.RefundAmount,
                Status = refundRequest.Status.ToString(),
                Message = "Full order refund approved and credited automatically."
            };

            return Result.Success(response, response.Message);
        }
        catch (InvalidOperationException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<RequestOrderRefundFromProposalResponse>(
                Error.InvalidValue,
                ex.Message);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(
                ex,
                "Error requesting full order refund from proposal {ProposalId}",
                request.ProposalId);
            return Result.Failure<RequestOrderRefundFromProposalResponse>(
                Error.ServerError,
                "An error occurred while requesting the full order refund.");
        }
    }

    private async Task<Result<RefundPolicyDefinition>> GetConfiguredPolicyAsync(
        CancellationToken cancellationToken)
    {
        var policyCodeSetting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == ChangeProposalSettingConstants.Group
                && setting.Scope.ToUpper() == ChangeProposalSettingConstants.RefundScope
                && setting.Code.ToUpper() == ChangeProposalSettingConstants.OrderRefundPolicyCode,
            cancellationToken);

        if (policyCodeSetting is null || string.IsNullOrWhiteSpace(policyCodeSetting.Value))
        {
            return Result.Failure<RefundPolicyDefinition>(
                Error.InvalidValue,
                "Change proposal order refund policy is not configured.");
        }

        var policyCode = policyCodeSetting.Value.Trim();
        var policySettings = await settingRepository.FindListAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == RefundPolicyConstants.Group
                && setting.Scope.ToLower() == policyCode.ToLower(),
            cancellationToken);

        if (!RefundPolicyDefinition.TryCreate(
                policyCode,
                policySettings,
                out var policy))
        {
            return Result.Failure<RefundPolicyDefinition>(
                Error.InvalidValue,
                "Configured change proposal order refund policy is not active or invalid.");
        }

        return Result.Success(policy!, "Refund policy retrieved successfully.");
    }

    private async Task NotifyManagerAsync(
        Guid managerId,
        Guid orderId,
        Guid proposalId,
        Guid userId,
        Guid refundRequestId,
        decimal refundAmount,
        CancellationToken cancellationToken)
    {
        if (managerId == Guid.Empty || managerId == userId)
            return;

        await businessNotificationService.NotifyAsync(
            NotificationTemplateKeys.ChangeProposalOrderRefundRequested,
            managerId,
            proposalId,
            new Dictionary<string, string>
            {
                ["referenceId"] = proposalId.ToString(),
                ["orderId"] = orderId.ToString(),
                ["refundAmount"] = refundAmount.ToString("0.##")
            },
            new
            {
                OrderId = orderId,
                ProposalId = proposalId,
                UserId = userId,
                RefundRequestId = refundRequestId,
                RefundAmount = refundAmount,
                Action = "RefundOrder"
            },
            cancellationToken);
    }
}
