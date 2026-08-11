using SC.Contract.Services.Notification;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Application.MediatR.RefundPolicy;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class RequestRefundFromProposalCommandHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IRefundAutoCreditService refundAutoCreditService,
    IBusinessNotificationService businessNotificationService) : ICommandHandler<RequestRefundFromProposalCommand, RequestRefundFromProposalResponse>
{
    public async Task<Result<RequestRefundFromProposalResponse>> Handle(RequestRefundFromProposalCommand request, CancellationToken cancellationToken)
    {
        var proposal = await proposalRepository.FindSingleAsync(
            p => p.Id == request.ProposalId,
            cancellationToken);

        if (proposal is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Proposal not found.");

        if (proposal.UserId != currentUserService.UserId)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.InvalidValue, "This proposal does not belong to you.");

        if (proposal.IsExpired(DateTimeOffset.UtcNow))
        {
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "Change proposal has expired.");
        }

        if (proposal.IsRequiredItem)
        {
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "Required item cannot be refunded separately. Please swap item or request a full order refund.");
        }

        var order = await orderRepository.FindSingleAsync(
            o => o.Id == proposal.OrderId && !o.IsDeleted,
            cancellationToken);

        if (order is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Order not found.");

        if (order.Status != OrderStatus.Preparing)
        {
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "Order is no longer available for change proposal actions.");
        }

        var session = await sessionRepository.FindSingleAsync(
            s => s.Id == order.SessionId && !s.IsDeleted,
            cancellationToken);

        if (session is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Session not found.");

        var item = order.OrderItems.FirstOrDefault(i => i.DishId == proposal.CurrentDishId);
        if (item is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Order item not found.");

        var policyResult = await GetConfiguredPolicyAsync(cancellationToken);
        if (policyResult.IsFailure)
            return Result.Failure<RequestRefundFromProposalResponse>(policyResult.Error!, policyResult.Message);

        var policy = policyResult.Value!;
        if (policy.RequiresImage)
        {
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "Configured change proposal item refund policy cannot require images.");
        }

        var itemAmount = item.UnitPrice.Amount * item.Quantity;
        if (itemAmount <= 0)
        {
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "Order item amount must be greater than zero.");
        }

        var refundRequest = RefundRequest.Submit(
            order.Id,
            currentUserService.UserId,
            policy.Scope,
            policy.Name,
            policy.Percent,
            itemAmount,
            $"Item refund requested from change proposal {proposal.Id}.");
        refundRequest.AttachProposalContext(
            item.Id,
            proposal.Id,
            item.DishId);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await refundLockService.LockOrderRefundRequestsAsync(order.Id, cancellationToken);

        var activeRequestExists = await refundRepository.ExistsAsync(
            refund =>
                !refund.IsDeleted
                && refund.OrderId == order.Id
                && (refund.Status == RefundRequestStatus.Pending
                    || refund.Status == RefundRequestStatus.Approved)
                && (!refund.OrderItemId.HasValue
                    || refund.OrderItemId == item.Id
                    || refund.ChangeProposalId == proposal.Id),
            cancellationToken);

        if (activeRequestExists)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<RequestRefundFromProposalResponse>(
                Error.InvalidValue,
                "This order item already has a pending or approved refund request.");
        }

        proposal.RequestRefund(currentUserService.UserId);
        item.MarkRefundPending();

        await refundRepository.AddAsync(refundRequest, cancellationToken);

        var creditResult = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
        if (creditResult.IsFailure)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure<RequestRefundFromProposalResponse>(
                creditResult.Error ?? Error.ServerError,
                creditResult.Message);
        }

        var credit = creditResult.Value!;

        item.CompleteRefund();

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
                refundRequest.OrderItemId,
                refundRequest.ChangeProposalId,
                refundRequest.DishId,
                refundRequest.RefundAmount,
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
            item.DishId,
            cancellationToken);

        var response = new RequestRefundFromProposalResponse
        {
            RefundRequestId = refundRequest.Id,
            OrderId = refundRequest.OrderId,
            OrderItemId = item.Id,
            DishId = item.DishId,
            PolicyCode = refundRequest.PolicyCode,
            RefundAmount = refundRequest.RefundAmount,
            Status = refundRequest.Status.ToString(),
            Message = "Item refund approved and credited automatically."
        };

        return Result.Success(response, response.Message);
    }

    private async Task<Result<RefundPolicyDefinition>> GetConfiguredPolicyAsync(
        CancellationToken cancellationToken)
    {
        var policyCodeSetting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == ChangeProposalSettingConstants.Group
                && setting.Scope.ToUpper() == ChangeProposalSettingConstants.RefundScope
                && setting.Code.ToUpper() == ChangeProposalSettingConstants.ItemRefundPolicyCode,
            cancellationToken);

        if (policyCodeSetting is null || string.IsNullOrWhiteSpace(policyCodeSetting.Value))
        {
            return Result.Failure<RefundPolicyDefinition>(
                Error.InvalidValue,
                "Change proposal item refund policy is not configured.");
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
                "Configured change proposal item refund policy is not active or invalid.");
        }

        return Result.Success(policy!, "Refund policy retrieved successfully.");
    }

    private async Task NotifyManagerAsync(
        Guid managerId,
        Guid orderId,
        Guid proposalId,
        Guid userId,
        Guid dishId,
        CancellationToken cancellationToken)
    {
        if (managerId == Guid.Empty || managerId == userId)
            return;

        await businessNotificationService.NotifyAsync(
            NotificationTemplateKeys.ChangeProposalItemRefundRequested,
            managerId,
            proposalId,
            new Dictionary<string, string>
            {
                ["referenceId"] = proposalId.ToString(),
                ["orderId"] = orderId.ToString()
            },
            new
            {
                OrderId = orderId,
                ProposalId = proposalId,
                UserId = userId,
                DishId = dishId,
                Action = "RefundItem"
            },
            cancellationToken);
    }
}
