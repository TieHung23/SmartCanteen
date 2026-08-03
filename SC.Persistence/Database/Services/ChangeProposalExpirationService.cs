using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Persistence.Database.Services;

public sealed class ChangeProposalExpirationService(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<Order, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    IRefundLockService refundLockService,
    IRefundAutoCreditService refundAutoCreditService,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService,
    ILogger<ChangeProposalExpirationService> logger)
    : IChangeProposalExpirationService
{
    private const string RefundPolicyGroup = "REFUND_POLICY";
    private const string RefundPolicyNameCode = "NAME";
    private const string RefundPolicyDescriptionCode = "DESCRIPTION";
    private const string RefundPolicyPercentCode = "PERCENT";
    private const string RefundPolicyRequiresImageCode = "REQUIRES_IMAGE";
    private const string ChangeProposalGroup = "CHANGE_PROPOSAL";
    private const string ChangeProposalRefundScope = "REFUND";
    private const string OrderRefundPolicyCode = "ORDER_REFUND_POLICY_CODE";
    private const string ItemRefundPolicyCode = "ITEM_REFUND_POLICY_CODE";

    public async Task<Result> ProcessExpiredProposalsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var proposals = await proposalRepository.FindListAsync(
            proposal => proposal.ProposalStatus == ChangeProposalStatus.WaitingResponse
                        && proposal.ExpiresAtUtc <= nowUtc,
            cancellationToken);

        foreach (var proposal in proposals)
        {
            try
            {
                await ProcessProposalAsync(proposal, cancellationToken);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                logger.LogError(
                    ex,
                    "Failed to process expired change proposal {ProposalId}",
                    proposal.Id);
            }
        }

        return Result.Success("Expired change proposals processed successfully.");
    }

    private async Task ProcessProposalAsync(
        OrderItemChangeProposal proposal,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.FindSingleAsync(
            order => order.Id == proposal.OrderId && !order.IsDeleted,
            query => query.Include(order => order.OrderItems),
            cancellationToken);

        if (order is null || order.Status != OrderStatus.Preparing)
        {
            return;
        }

        if (proposal.IsRequiredItem)
        {
            await ProcessRequiredItemTimeoutAsync(proposal, order, cancellationToken);
        }
        else
        {
            await ProcessOptionalItemTimeoutAsync(proposal, order, cancellationToken);
        }
    }

    private async Task ProcessRequiredItemTimeoutAsync(
        OrderItemChangeProposal proposal,
        Order order,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await refundLockService.LockOrderRefundRequestsAsync(order.Id, cancellationToken);

        var activeRequestExists = await HasActiveOrderRefundRequestAsync(order.Id, cancellationToken);
        RefundRequest? refundRequest = null;
        RefundAutoCreditResult? creditResult = null;

        if (!activeRequestExists)
        {
            var policy = await GetConfiguredPolicyAsync(
                OrderRefundPolicyCode,
                cancellationToken);
            var orderAmount = CalculateOrderAmount(order);
            if (orderAmount <= 0)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            refundRequest = RefundRequest.Submit(
                order.Id,
                proposal.UserId,
                policy.Scope,
                policy.Name,
                policy.Percent,
                orderAmount,
                $"Automatic full order refund because change proposal {proposal.Id} expired.");
            refundRequest.AttachProposalContext(
                orderItemId: null,
                changeProposalId: proposal.Id,
                dishId: proposal.CurrentDishId);
            await refundRepository.AddAsync(refundRequest, cancellationToken);

            var creditOutcome = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
            if (creditOutcome.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                logger.LogError(
                    "Failed to auto-credit refund {RefundRequestId} for expired proposal {ProposalId}: {Message}",
                    refundRequest.Id,
                    proposal.Id,
                    creditOutcome.Message);
                return;
            }

            creditResult = creditOutcome.Value;
        }

        proposal.RequestOrderRefund(proposal.UserId);
        var siblingProposals = await proposalRepository.FindListAsync(
            sibling =>
                sibling.OrderId == order.Id
                && sibling.Id != proposal.Id
                && sibling.ProposalStatus == ChangeProposalStatus.WaitingResponse,
            cancellationToken);

        foreach (var sibling in siblingProposals)
        {
            sibling.RequestOrderRefund(proposal.UserId);
            proposalRepository.Update(sibling);
        }

        var fromStatus = order.Status;
        if (fromStatus != OrderStatus.Cancelled)
        {
            order.UpdateStatus(OrderStatus.Cancelled, proposal.UserId);
            await orderStatusHistoryRepository.AddAsync(
                OrderStatusHistoryEntity.Create(
                    order.Id,
                    fromStatus,
                    OrderStatus.Cancelled,
                    proposal.UserId,
                    "ChangeProposalExpiredOrderRefund",
                    "Change proposal response window expired."),
                cancellationToken);
        }

        proposalRepository.Update(proposal);
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        if (refundRequest is not null && creditResult is not null)
        {
            await NotifyRefundApprovedAsync(refundRequest, creditResult.Value, cancellationToken);
        }
        else
        {
            await NotifyCustomerAsync(
                NotificationTemplateKeys.ChangeProposalOrderRefundRequested,
                proposal.UserId,
                proposal.Id,
                order.Id,
                refundRequest?.Id,
                refundRequest?.RefundAmount ?? 0,
                "RefundOrder",
                cancellationToken);
        }
    }

    private async Task ProcessOptionalItemTimeoutAsync(
        OrderItemChangeProposal proposal,
        Order order,
        CancellationToken cancellationToken)
    {
        var item = order.OrderItems.FirstOrDefault(item => item.DishId == proposal.CurrentDishId);
        if (item is null)
        {
            return;
        }

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

        RefundRequest? refundRequest = null;
        RefundAutoCreditResult? creditResult = null;
        if (!activeRequestExists)
        {
            var policy = await GetConfiguredPolicyAsync(
                ItemRefundPolicyCode,
                cancellationToken);
            var itemAmount = item.UnitPrice.Amount * item.Quantity;
            if (itemAmount <= 0)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            refundRequest = RefundRequest.Submit(
                order.Id,
                proposal.UserId,
                policy.Scope,
                policy.Name,
                policy.Percent,
                itemAmount,
                $"Automatic item refund because change proposal {proposal.Id} expired.");
            refundRequest.AttachProposalContext(
                item.Id,
                proposal.Id,
                item.DishId);
            await refundRepository.AddAsync(refundRequest, cancellationToken);

            var creditOutcome = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
            if (creditOutcome.IsFailure)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                logger.LogError(
                    "Failed to auto-credit refund {RefundRequestId} for expired proposal {ProposalId}: {Message}",
                    refundRequest.Id,
                    proposal.Id,
                    creditOutcome.Message);
                return;
            }

            creditResult = creditOutcome.Value;
        }

        proposal.RequestRefund(proposal.UserId);
        if (item.ItemStatus == OrderItemStatus.ChangePending)
        {
            item.MarkRefundPending();
        }

        if (refundRequest is not null && item.ItemStatus == OrderItemStatus.RefundPending)
        {
            item.CompleteRefund();
        }

        proposalRepository.Update(proposal);
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        if (refundRequest is not null && creditResult is not null)
        {
            await NotifyRefundApprovedAsync(refundRequest, creditResult.Value, cancellationToken);
        }
        else
        {
            await NotifyCustomerAsync(
                NotificationTemplateKeys.ChangeProposalItemRefundRequested,
                proposal.UserId,
                proposal.Id,
                order.Id,
                refundRequest?.Id,
                refundRequest?.RefundAmount ?? 0,
                "RefundItem",
                cancellationToken);
        }
    }

    private async Task<bool> HasActiveOrderRefundRequestAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await refundRepository.ExistsAsync(
            refund =>
                !refund.IsDeleted
                && refund.OrderId == orderId
                && !refund.OrderItemId.HasValue
                && (refund.Status == RefundRequestStatus.Pending
                    || refund.Status == RefundRequestStatus.Approved),
            cancellationToken);
    }

    private async Task<TimeoutRefundPolicy> GetConfiguredPolicyAsync(
        string policyCodeSettingCode,
        CancellationToken cancellationToken)
    {
        var policyCodeSetting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == ChangeProposalGroup
                && setting.Scope.ToUpper() == ChangeProposalRefundScope
                && setting.Code.ToUpper() == policyCodeSettingCode,
            cancellationToken);

        if (policyCodeSetting is null || string.IsNullOrWhiteSpace(policyCodeSetting.Value))
        {
            throw new InvalidOperationException(
                $"Change proposal refund policy setting {policyCodeSettingCode} is not configured.");
        }

        var policyCode = policyCodeSetting.Value.Trim();
        var policySettings = await settingRepository.FindListAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == RefundPolicyGroup
                && setting.Scope.ToLower() == policyCode.ToLower(),
            cancellationToken);

        if (!TryCreatePolicy(policyCode, policySettings, out var policy))
        {
            throw new InvalidOperationException(
                $"Configured change proposal refund policy {policyCode} is not active or invalid.");
        }

        if (policy!.RequiresImage)
        {
            throw new InvalidOperationException(
                $"Configured change proposal refund policy {policyCode} cannot require images.");
        }

        return policy;
    }

    private static bool TryCreatePolicy(
        string scope,
        IEnumerable<SettingAggregate> settings,
        out TimeoutRefundPolicy? policy)
    {
        policy = null;
        var values = settings
            .Where(setting => !setting.IsDeleted)
            .GroupBy(setting => setting.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue(RefundPolicyNameCode, out var nameSetting)
            || string.IsNullOrWhiteSpace(nameSetting.Value)
            || !values.TryGetValue(RefundPolicyDescriptionCode, out _)
            || !values.TryGetValue(RefundPolicyPercentCode, out var percentSetting)
            || !decimal.TryParse(
                percentSetting.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var percent)
            || percent <= 0
            || percent > 100
            || !values.TryGetValue(RefundPolicyRequiresImageCode, out var requiresImageSetting)
            || !bool.TryParse(requiresImageSetting.Value, out var requiresImage))
        {
            return false;
        }

        policy = new TimeoutRefundPolicy(
            scope,
            nameSetting.Value.Trim(),
            percent,
            requiresImage);

        return true;
    }

    private async Task NotifyRefundApprovedAsync(
        RefundRequest refund,
        RefundAutoCreditResult credit,
        CancellationToken cancellationToken)
    {
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
                BalanceAfter = credit.BalanceAfter,
                WalletTransactionId = credit.WalletTransactionId
            },
            cancellationToken);
    }

    private async Task NotifyCustomerAsync(
        string templateKey,
        Guid userId,
        Guid proposalId,
        Guid orderId,
        Guid? refundRequestId,
        decimal refundAmount,
        string action,
        CancellationToken cancellationToken)
    {
        await businessNotificationService.NotifyAsync(
            templateKey,
            userId,
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
                RefundRequestId = refundRequestId,
                RefundAmount = refundAmount,
                Action = action,
                Reason = "ChangeProposalExpired"
            },
            cancellationToken);
    }

    private static decimal CalculateOrderAmount(Order order)
    {
        return order.OrderItems
            .Where(item => item.ItemStatus != OrderItemStatus.Refunded)
            .Sum(item => item.UnitPrice.Amount * item.Quantity);
    }

    private sealed record TimeoutRefundPolicy(
        string Scope,
        string Name,
        decimal Percent,
        bool RequiresImage);
}
