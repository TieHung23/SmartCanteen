using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Dish;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.Refund.AggregateRoot;
using SC.Domain.Domain.Refund.Enum;
using SC.Domain.Domain.Session.AggregateRoot;
using SC.Domain.Domain.Session.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderItemEntity = SC.Domain.Domain.Order.Entity.OrderItem;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;
using SettingAggregate = SC.Domain.Domain.Setting.AggregateRoot.Setting;

namespace SC.Persistence.Database.Services;

public class FinalizeSessionService(
    SmartCanteenDbContext context,
    IGenericRepository<Session, Guid> sessionRepository,
    IGenericRepository<Order, Guid> orderRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<RefundRequest, Guid> refundRepository,
    IGenericRepository<SettingAggregate, Guid> settingRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IUnitOfWork unitOfWork,
    IRefundLockService refundLockService,
    IRefundAutoCreditService refundAutoCreditService,
    IBusinessNotificationService businessNotificationService,
    ILogger<FinalizeSessionService> logger) : IFinalizeSessionService
{
    private const string RefundPolicyGroup = "REFUND_POLICY";
    private const string RefundPolicyNameCode = "NAME";
    private const string RefundPolicyDescriptionCode = "DESCRIPTION";
    private const string RefundPolicyPercentCode = "PERCENT";
    private const string RefundPolicyRequiresImageCode = "REQUIRES_IMAGE";
    private const string ChangeProposalGroup = "CHANGE_PROPOSAL";
    private const string ChangeProposalRefundScope = "REFUND";
    private const string ChangeProposalResponseScope = "RESPONSE";
    private const string ChangeProposalOrderRefundPolicyCode = "ORDER_REFUND_POLICY_CODE";
    private const string ChangeProposalResponseWindowMinutesCode = "RESPONSE_WINDOW_MINUTES";
    private const int DefaultChangeProposalResponseWindowMinutes = 30;

    public async Task<Result> FinalizeAsync(Guid sessionId, List<(Guid DishId, int PreparedQuantity, Guid? SuggestedDishId)> preparedDishes, Guid managerId, CancellationToken cancellationToken = default, bool startServingNow = false)
    {
        var session = await sessionRepository.FindSingleAsync(
            s => s.Id == sessionId && !s.IsDeleted,
            q => q.Include(s => s.SessionDishes)
                .Include(s => s.MealTemplates)
                .ThenInclude(t => t.Settings),
            cancellationToken);

        if (session is null)
            return Result.Failure(Error.NullValue, "Session not found.");

        if (session.IsFinalized)
            return Result.Failure(Error.InvalidValue, "Session is already finalized.");

        if (preparedDishes.GroupBy(d => d.DishId).Any(group => group.Count() > 1))
            return Result.Failure(Error.InvalidValue, "Prepared dishes cannot contain duplicate dish IDs.");

        try
        {
            if (session.FinalizationDeadline.HasValue && DateTimeOffset.UtcNow > session.FinalizationDeadline.Value)
                return Result.Failure(Error.InvalidValue, "Finalization deadline has passed.");
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.InvalidValue, ex.Message);
        }

        if (startServingNow)
        {
            var now = DateTimeOffset.UtcNow;

            if (session.AvailableTo <= now)
                return Result.Failure(Error.InvalidValue, "Session's available window has already ended; cannot start serving now.");

            // Pulling AvailableFrom to now widens this session's window to [now, session.AvailableTo].
            // Check that widened window against every other session's *full* window (not just "is it
            // active right now") - otherwise a session that hasn't started yet but falls inside the
            // widened window would slip through and end up overlapping once it does start.
            var hasOverlappingSession = await sessionRepository.ExistsAsync(
                x => !x.IsDeleted
                     && x.Id != session.Id
                     && x.AvailableFrom < session.AvailableTo
                     && now < x.AvailableTo,
                cancellationToken);

            if (hasOverlappingSession)
                return Result.Failure(Error.InvalidValue, "This session's serving window would overlap with another session; sessions are not allowed to overlap.");
        }

        foreach (var input in preparedDishes)
        {
            var sd = session.SessionDishes.FirstOrDefault(x => x.DishId == input.DishId);
            if (sd is null)
                return Result.Failure(Error.NullValue, $"Dish {input.DishId} is not part of this session.");

            sd.SetPreparedQuantity(input.PreparedQuantity);
        }

        var orders = await orderRepository.FindListAsync(
            o => o.SessionId == sessionId,
            cancellationToken);

        var totalOrderedByDish = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.DishId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var suggestedDishByDish = preparedDishes
            .Where(d => d.SuggestedDishId.HasValue)
            .ToDictionary(d => d.DishId, d => d.SuggestedDishId);

        var orderedDishIds = totalOrderedByDish.Keys.ToList();
        var suggestedDishIds = suggestedDishByDish.Values
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        var referencedDishIds = orderedDishIds
            .Concat(suggestedDishIds)
            .Concat(session.SessionDishes.Select(sd => sd.DishId))
            .Distinct()
            .ToList();

        var dishes = await dishRepository.FindListAsync(
            d => referencedDishIds.Contains(d.Id),
            cancellationToken);
        var dishMap = dishes.ToDictionary(d => d.Id);
        var dishCategoryByDish = dishMap.ToDictionary(d => d.Key, d => d.Value.CategoryId);
        var pendingNotifications = new List<ChangeProposalNotification>();
        var proposalExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(
            await GetChangeProposalResponseWindowMinutesAsync(cancellationToken));

        var suggestedDishValidation = ValidateSuggestedDishes(
            session,
            orders,
            suggestedDishByDish,
            dishMap,
            dishCategoryByDish);

        if (suggestedDishValidation.IsFailure)
            return suggestedDishValidation;

        // Category-level prepared-quantity gate: a category's total CB (summed across every dish
        // in that category, including dishes nobody ordered - they can still donate spare CB) must
        // cover that category's total ordered quantity. Only categories that were actually ordered
        // are checked. This blocks the whole finalize call before anything is mutated/persisted -
        // the manager must adjust CB and retry, rather than silently letting some orders through.
        // Runs after the suggested-dish validation so malformed manager input is still reported as
        // such rather than being masked by a quantity complaint.
        var orderedByCategory = totalOrderedByDish
            .Where(kv => dishCategoryByDish.ContainsKey(kv.Key))
            .GroupBy(kv => dishCategoryByDish[kv.Key])
            .ToDictionary(g => g.Key, g => g.Sum(kv => kv.Value));

        var preparedByCategory = session.SessionDishes
            .Where(sd => dishCategoryByDish.ContainsKey(sd.DishId))
            .GroupBy(sd => dishCategoryByDish[sd.DishId])
            .ToDictionary(g => g.Key, g => g.Sum(sd => sd.PreparedQuantity ?? 0));

        foreach (var (categoryId, orderedQty) in orderedByCategory)
        {
            if (orderedQty <= 0)
                continue;

            var preparedQty = preparedByCategory.GetValueOrDefault(categoryId);
            if (preparedQty < orderedQty)
            {
                return Result.Failure(
                    Error.InvalidValue,
                    $"Prepared quantity for category {categoryId} ({preparedQty}) is less than the total ordered quantity ({orderedQty}); finalize is blocked.");
            }
        }

        // Per-category FIFO allocation: within each category, order items are honored against
        // their own dish's CB in order-creation sequence (earliest orders first). An item that
        // doesn't fit its own dish's remaining CB is entirely change-pending (an OrderItem cannot
        // be partially confirmed) and gets auto-matched to whichever category-mate dish still has
        // the most spare CB, so the manager only has to budget per category rather than pick a swap
        // target dish by dish.
        //
        // An item that swaps away consumes the donor's CB, never its own dish's - so the portions
        // left under its own dish stay available for later orders of that same dish. Combined with
        // the category gate, that keeps a donor available for every shortfall: before any item is
        // processed the category's total remaining CB is at least that item's quantity, so if the
        // item's own dish is short then some other dish must still hold a positive remainder.
        //
        // The donor is a suggestion, not a reservation - a customer is free to pick a different
        // dish when accepting - so a donor may be suggested to more portions than it has spare.
        var decisions = new Dictionary<OrderItemEntity, (bool Confirm, Guid? AutoSuggestedDishId)>();

        foreach (var categoryGroup in orders
                     .SelectMany(order => order.OrderItems.Select(item => (order, item)))
                     .Where(x => dishCategoryByDish.ContainsKey(x.item.DishId))
                     .GroupBy(x => dishCategoryByDish[x.item.DishId]))
        {
            var remainingByDish = session.SessionDishes
                .Where(sd => dishCategoryByDish.GetValueOrDefault(sd.DishId) == categoryGroup.Key)
                .ToDictionary(sd => sd.DishId, sd => sd.PreparedQuantity ?? 0);

            foreach (var (order, item) in categoryGroup
                         .OrderBy(x => x.order.CreatedAtUtc)
                         .ThenBy(x => x.order.Id))
            {
                var ownRemaining = remainingByDish.GetValueOrDefault(item.DishId);
                if (ownRemaining >= item.Quantity)
                {
                    remainingByDish[item.DishId] = ownRemaining - item.Quantity;
                    decisions[item] = (true, null);
                }
                else
                {
                    var donor = remainingByDish
                        .Where(kv => kv.Key != item.DishId && kv.Value > 0)
                        .OrderByDescending(kv => kv.Value)
                        .ThenBy(kv => dishMap.GetValueOrDefault(kv.Key)?.Name)
                        .ThenBy(kv => kv.Key)
                        .Select(kv => (Guid?)kv.Key)
                        .FirstOrDefault();

                    if (donor.HasValue)
                        remainingByDish[donor.Value] -= item.Quantity;

                    decisions[item] = (false, donor);
                }
            }
        }

        foreach (var order in orders)
        {
            foreach (var item in order.OrderItems)
            {
                var (shouldConfirm, autoSuggestedDishId) = decisions.TryGetValue(item, out var decision)
                    ? decision
                    : (Confirm: true, AutoSuggestedDishId: (Guid?)null);

                if (shouldConfirm)
                {
                    item.Confirm();
                }
                else
                {
                    item.MarkChangePending();
                    var requiredCategoryId = GetRequiredCategoryId(
                        session,
                        order.MealTemplateId,
                        item.DishId,
                        dishCategoryByDish);

                    // Manager-supplied suggestion (validated above) always wins; the auto-computed
                    // category-mate donor only fills in when the manager left it unset.
                    var suggestedDishId =
                        suggestedDishByDish.TryGetValue(item.DishId, out var managerSuggested) && managerSuggested.HasValue
                            ? managerSuggested
                            : autoSuggestedDishId;

                    var proposal = OrderItemChangeProposal.Create(
                        order.Id,
                        order.CreatedBy,
                        item.DishId,
                        suggestedDishId,
                        requiredCategoryId.HasValue,
                        requiredCategoryId,
                        proposalExpiresAtUtc);

                    await proposalRepository.AddAsync(proposal, cancellationToken);
                    pendingNotifications.Add(CreateNotification(order, item.DishId, proposal, dishMap));
                }
            }

            // Same safety net as AutoConfirmAll: Order.Status is normally driven by the robot/pickup
            // pipeline (set to Preparing by CreateServingJob right when the order is created), not by
            // finalize. That call is best-effort - if it silently failed, the order would be stuck at
            // Pending forever with no serving job and no other path back, even though its items just
            // got confirmed/change-pending here. Applies regardless of the per-item outcome, since the
            // order is now actively being handled either way.
            if (order.Status == OrderStatus.Pending)
            {
                var fromStatus = order.Status;
                order.UpdateStatus(OrderStatus.Preparing, managerId);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(
                        order.Id,
                        fromStatus,
                        OrderStatus.Preparing,
                        managerId,
                        "FinalizeConfirm",
                        "Session finalized; order was still Pending so it was moved to Preparing."),
                    cancellationToken);
            }

            orderRepository.Update(order);
        }

        session.Finalize(managerId);

        if (startServingNow)
            session.StartServingNow(managerId);

        await context.SaveChangesAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.ChangeProposalCreated,
                notification.UserId,
                notification.ProposalId,
                new Dictionary<string, string>
                {
                    ["referenceId"] = notification.ProposalId.ToString(),
                    ["orderId"] = notification.OrderId.ToString(),
                    ["dishName"] = notification.CurrentDishName
                },
                new
                {
                    notification.OrderId,
                    notification.ProposalId,
                    notification.CurrentDishId,
                    notification.CurrentDishName,
                    notification.SuggestedDishId,
                    notification.SuggestedDishName,
                    notification.IsRequiredItem,
                    notification.RequiredCategoryId,
                    notification.AllowedActions
                },
                cancellationToken);
        }

        return Result.Success("Session finalized successfully.");
    }

    private static ChangeProposalNotification CreateNotification(
        Order order,
        Guid currentDishId,
        OrderItemChangeProposal proposal,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishMap)
    {
        dishMap.TryGetValue(currentDishId, out var currentDish);

        DishAggregateRoot? suggestedDish = null;
        if (proposal.SuggestedDishId.HasValue)
            dishMap.TryGetValue(proposal.SuggestedDishId.Value, out suggestedDish);

        var allowedActions = proposal.IsRequiredItem
            ? new[] { "SwapItem", "RefundOrder" }
            : new[] { "SwapItem", "RefundItem", "RefundOrder" };

        return new ChangeProposalNotification(
            order.Id,
            order.CreatedBy,
            proposal.Id,
            currentDishId,
            currentDish?.Name ?? "Selected dish",
            proposal.SuggestedDishId,
            suggestedDish?.Name,
            proposal.IsRequiredItem,
            proposal.RequiredCategoryId,
            allowedActions);
    }

    private static Result ValidateSuggestedDishes(
        Session session,
        IReadOnlyCollection<Order> orders,
        IReadOnlyDictionary<Guid, Guid?> suggestedDishByDish,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishMap,
        IReadOnlyDictionary<Guid, Guid> dishCategoryByDish)
    {
        var sessionDishIds = session.SessionDishes
            .Select(sd => sd.DishId)
            .ToHashSet();

        foreach (var (currentDishId, suggestedDishId) in suggestedDishByDish)
        {
            if (!suggestedDishId.HasValue)
                continue;

            if (suggestedDishId.Value == currentDishId)
            {
                return Result.Failure(
                    Error.InvalidValue,
                    $"Suggested dish for {currentDishId} must be different from the current dish.");
            }

            if (!sessionDishIds.Contains(suggestedDishId.Value))
            {
                return Result.Failure(
                    Error.InvalidValue,
                    $"Suggested dish {suggestedDishId.Value} is not part of this session.");
            }

            if (!dishMap.TryGetValue(suggestedDishId.Value, out var suggestedDish)
                || suggestedDish.IsDeleted
                || !suggestedDish.IsActive)
            {
                return Result.Failure(
                    Error.NullValue,
                    $"Suggested dish {suggestedDishId.Value} was not found or is inactive.");
            }

            if (!dishMap.TryGetValue(currentDishId, out var currentDish)
                || currentDish.IsDeleted)
            {
                return Result.Failure(
                    Error.NullValue,
                    $"Current dish {currentDishId} was not found.");
            }

            var affectedOrders = orders
                .Where(order => order.OrderItems.Any(item => item.DishId == currentDishId))
                .ToList();

            foreach (var order in affectedOrders)
            {
                if (!IsCategoryAllowedForOrderTemplate(session, order.MealTemplateId, suggestedDish.CategoryId))
                {
                    return Result.Failure(
                        Error.InvalidValue,
                        $"Suggested dish {suggestedDishId.Value} is not allowed by one or more affected order templates.");
                }

                var requiredCategoryId = GetRequiredCategoryId(
                    session,
                    order.MealTemplateId,
                    currentDishId,
                    dishCategoryByDish);

                if (requiredCategoryId.HasValue && suggestedDish.CategoryId != requiredCategoryId.Value)
                {
                    return Result.Failure(
                        Error.InvalidValue,
                        $"Suggested dish {suggestedDishId.Value} must be in the same required category as dish {currentDishId}.");
                }

                if (!requiredCategoryId.HasValue && suggestedDish.CategoryId != currentDish.CategoryId)
                {
                    return Result.Failure(
                        Error.InvalidValue,
                        $"Suggested dish {suggestedDishId.Value} must be in the same category as optional dish {currentDishId}.");
                }
            }
        }

        return Result.Success("Suggested dishes are valid.");
    }

    private static bool IsCategoryAllowedForOrderTemplate(
        Session session,
        Guid? mealTemplateId,
        Guid categoryId)
    {
        if (!mealTemplateId.HasValue)
            return true;

        var template = session.MealTemplates.FirstOrDefault(t => t.Id == mealTemplateId.Value && !t.IsDeleted);
        return template is null
            || template.Settings.Any(s => s.CategoryId == categoryId && !s.IsDeleted);
    }

    private static Guid? GetRequiredCategoryId(
        Session session,
        Guid? mealTemplateId,
        Guid dishId,
        IReadOnlyDictionary<Guid, Guid> dishCategoryByDish)
    {
        if (!mealTemplateId.HasValue || !dishCategoryByDish.TryGetValue(dishId, out var categoryId))
            return null;

        var template = session.MealTemplates.FirstOrDefault(t => t.Id == mealTemplateId.Value && !t.IsDeleted);
        var setting = template?.Settings.FirstOrDefault(s => s.CategoryId == categoryId && !s.IsDeleted);

        return setting is { IsRequired: true } ? categoryId : null;
    }

    private async Task<int> GetChangeProposalResponseWindowMinutesAsync(
        CancellationToken cancellationToken)
    {
        var setting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == ChangeProposalGroup
                && setting.Scope.ToUpper() == ChangeProposalResponseScope
                && setting.Code.ToUpper() == ChangeProposalResponseWindowMinutesCode,
            cancellationToken);

        if (setting is null
            || !int.TryParse(setting.Value, out var minutes)
            || minutes <= 0)
        {
            return DefaultChangeProposalResponseWindowMinutes;
        }

        return minutes;
    }

    public async Task AutoFinalizeOverdueSessionsAsync(CancellationToken cancellationToken = default)
    {
        var overdueSessions = await sessionRepository.FindListAsync(
            s => !s.IsDeleted
                 && !s.IsFinalized
                 && s.FinalizationDeadline.HasValue
                 && s.FinalizationDeadline <= DateTimeOffset.UtcNow,
            q => q.Include(s => s.SessionDishes),
            cancellationToken);

        var pendingNotifications = new List<AutoFinalizeNotification>();

        foreach (var session in overdueSessions)
        {
            try
            {
                var orders = await orderRepository.FindListAsync(
                    o => o.SessionId == session.Id,
                    cancellationToken);

                if (session.AutoFinalizePolicy == AutoFinalizePolicy.AutoReject)
                {
                    var refundPolicy = await GetConfiguredOrderRefundPolicyAsync(cancellationToken);
                    await AutoRejectSessionAsync(session, orders, refundPolicy, pendingNotifications, cancellationToken);
                }
                else if (session.AutoFinalizePolicy == AutoFinalizePolicy.AutoConfirmAll)
                {
                    await AutoConfirmAllSession(session, orders, pendingNotifications, cancellationToken);
                }

                session.AutoFinalize();
                sessionRepository.Update(session);

                logger.LogInformation(
                    "Auto-finalized session {SessionId} with policy {Policy}",
                    session.Id, session.AutoFinalizePolicy);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to auto-finalize session {SessionId}",
                    session.Id);
            }
        }

        if (overdueSessions.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        foreach (var notification in pendingNotifications)
        {
            await businessNotificationService.NotifyAsync(
                notification.TemplateKey,
                notification.UserId,
                notification.ReferenceId,
                notification.Tokens,
                notification.Data,
                cancellationToken);
        }
    }

    private async Task AutoRejectSessionAsync(
        Session session,
        IReadOnlyCollection<Order> orders,
        AutoOrderRefundPolicy refundPolicy,
        ICollection<AutoFinalizeNotification> pendingNotifications,
        CancellationToken cancellationToken)
    {
        foreach (var order in orders)
        {
            try
            {
                await AutoRejectOrderAsync(order, session.Id, refundPolicy, pendingNotifications, cancellationToken);
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                logger.LogError(
                    ex,
                    "Failed to auto-reject order {OrderId} for session {SessionId}",
                    order.Id,
                    session.Id);
            }
        }
    }

    private async Task AutoRejectOrderAsync(
        Order order,
        Guid sessionId,
        AutoOrderRefundPolicy refundPolicy,
        ICollection<AutoFinalizeNotification> pendingNotifications,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await refundLockService.LockOrderRefundRequestsAsync(order.Id, cancellationToken);

        foreach (var item in order.OrderItems)
        {
            if (item.ItemStatus is OrderItemStatus.Pending or OrderItemStatus.ChangePending)
            {
                item.RefundItem();
            }
        }

        var fromStatus = order.Status;
        if (fromStatus != OrderStatus.Cancelled)
        {
            order.UpdateStatus(OrderStatus.Cancelled, order.CreatedBy);
            await orderStatusHistoryRepository.AddAsync(
                OrderStatusHistoryEntity.Create(
                    order.Id,
                    fromStatus,
                    OrderStatus.Cancelled,
                    order.CreatedBy,
                    "AutoFinalizeReject",
                    "Session finalization deadline passed."),
                cancellationToken);
        }

        orderRepository.Update(order);

        if (order.WalletTransactionId.HasValue)
        {
            await CreateAndCreditAutoRejectRefundIfNeededAsync(
                order,
                sessionId,
                refundPolicy,
                pendingNotifications,
                cancellationToken);
        }
        else
        {
            pendingNotifications.Add(CreateAutoRejectCancelledNotification(order, sessionId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task CreateAndCreditAutoRejectRefundIfNeededAsync(
        Order order,
        Guid sessionId,
        AutoOrderRefundPolicy refundPolicy,
        ICollection<AutoFinalizeNotification> pendingNotifications,
        CancellationToken cancellationToken)
    {
        var activeRequestExists = await refundRepository.ExistsAsync(
            refund =>
                !refund.IsDeleted
                && refund.OrderId == order.Id
                && (refund.Status == RefundRequestStatus.Pending
                    || refund.Status == RefundRequestStatus.Approved),
            cancellationToken);

        if (activeRequestExists)
        {
            pendingNotifications.Add(CreateAutoRejectCancelledNotification(order, sessionId));
            return;
        }

        var orderAmount = order.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);
        if (orderAmount <= 0)
        {
            pendingNotifications.Add(CreateAutoRejectCancelledNotification(order, sessionId));
            return;
        }

        var refundRequest = RefundRequest.Submit(
            order.Id,
            order.CreatedBy,
            refundPolicy.Scope,
            refundPolicy.Name,
            refundPolicy.Percent,
            orderAmount,
            $"Automatic full order refund because session {sessionId} was not finalized before deadline.");

        await refundRepository.AddAsync(refundRequest, cancellationToken);

        var creditResult = await refundAutoCreditService.CreditAsync(refundRequest, cancellationToken);
        if (creditResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to auto-credit refund for order {order.Id}: {creditResult.Message}");
        }

        pendingNotifications.Add(CreateAutoRejectRefundNotification(order, sessionId, refundRequest));
    }

    private async Task AutoConfirmAllSession(
        Session session,
        IReadOnlyCollection<Order> orders,
        ICollection<AutoFinalizeNotification> pendingNotifications,
        CancellationToken cancellationToken)
    {
        var totalOrderedByDish = orders
            .SelectMany(order => order.OrderItems)
            .GroupBy(item => item.DishId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        foreach (var sessionDish in session.SessionDishes)
        {
            sessionDish.SetPreparedQuantity(totalOrderedByDish.GetValueOrDefault(sessionDish.DishId));
        }

        foreach (var order in orders)
        {
            foreach (var item in order.OrderItems)
            {
                if (item.ItemStatus == OrderItemStatus.Pending)
                {
                    item.Confirm();
                }
            }

            // Order.Status is normally driven by the robot/pickup pipeline (set to Preparing by
            // CreateServingJob right when the order is created), not by finalize. But that call is
            // best-effort - if it silently failed, the order would be stuck at Pending forever with
            // no serving job and no other path back. Auto-confirming its items without ever moving it
            // out of Pending here would leave it permanently invisible to robots despite being paid
            // for, so bring it into Preparing as a fallback whenever it's still Pending at this point.
            if (order.Status == OrderStatus.Pending)
            {
                var fromStatus = order.Status;
                order.UpdateStatus(OrderStatus.Preparing, order.CreatedBy);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(
                        order.Id,
                        fromStatus,
                        OrderStatus.Preparing,
                        order.CreatedBy,
                        "AutoFinalizeConfirm",
                        "Session auto-finalized with AutoConfirmAll; order was still Pending so it was moved to Preparing."),
                    cancellationToken);
            }

            orderRepository.Update(order);
            pendingNotifications.Add(CreateAutoConfirmAllNotification(order, session.Id));
        }
    }

    private async Task<AutoOrderRefundPolicy> GetConfiguredOrderRefundPolicyAsync(
        CancellationToken cancellationToken)
    {
        var policyCodeSetting = await settingRepository.FindSingleAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == ChangeProposalGroup
                && setting.Scope.ToUpper() == ChangeProposalRefundScope
                && setting.Code.ToUpper() == ChangeProposalOrderRefundPolicyCode,
            cancellationToken);

        if (policyCodeSetting is null || string.IsNullOrWhiteSpace(policyCodeSetting.Value))
            throw new InvalidOperationException("Change proposal order refund policy is not configured.");

        var policyCode = policyCodeSetting.Value.Trim();
        var policySettings = await settingRepository.FindListAsync(
            setting =>
                !setting.IsDeleted
                && setting.Group.ToUpper() == RefundPolicyGroup
                && setting.Scope.ToLower() == policyCode.ToLower(),
            cancellationToken);

        if (!TryCreateRefundPolicy(policyCode, policySettings, out var policy))
            throw new InvalidOperationException("Configured change proposal order refund policy is not active or invalid.");

        if (policy!.RequiresImage)
            throw new InvalidOperationException("Configured change proposal order refund policy cannot require images.");

        return policy;
    }

    private static bool TryCreateRefundPolicy(
        string scope,
        IEnumerable<SettingAggregate> settings,
        out AutoOrderRefundPolicy? policy)
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

        policy = new AutoOrderRefundPolicy(
            scope,
            nameSetting.Value.Trim(),
            percent,
            requiresImage);

        return true;
    }

    private static AutoFinalizeNotification CreateAutoRejectRefundNotification(
        Order order,
        Guid sessionId,
        RefundRequest refundRequest)
    {
        return new AutoFinalizeNotification(
            NotificationTemplateKeys.SessionAutoRejected,
            order.CreatedBy,
            order.Id,
            new Dictionary<string, string>
            {
                ["referenceId"] = order.Id.ToString(),
                ["refundAmount"] = refundRequest.RefundAmount.ToString("0.##")
            },
            new
            {
                SessionId = sessionId,
                OrderId = order.Id,
                RefundRequestId = refundRequest.Id,
                refundRequest.RefundAmount,
                Status = refundRequest.Status.ToString()
            });
    }

    private static AutoFinalizeNotification CreateAutoRejectCancelledNotification(
        Order order,
        Guid sessionId)
    {
        return new AutoFinalizeNotification(
            NotificationTemplateKeys.SessionAutoRejected,
            order.CreatedBy,
            order.Id,
            new Dictionary<string, string>
            {
                ["referenceId"] = order.Id.ToString(),
                ["refundAmount"] = "0"
            },
            new
            {
                SessionId = sessionId,
                OrderId = order.Id,
                RefundRequestId = (Guid?)null,
                RefundAmount = 0,
                Status = OrderStatus.Cancelled.ToString()
            });
    }

    private static AutoFinalizeNotification CreateAutoConfirmAllNotification(
        Order order,
        Guid sessionId)
    {
        return new AutoFinalizeNotification(
            NotificationTemplateKeys.SessionAutoConfirmed,
            order.CreatedBy,
            order.Id,
            new Dictionary<string, string>
            {
                ["referenceId"] = order.Id.ToString()
            },
            new
            {
                SessionId = sessionId,
                OrderId = order.Id,
                Status = "Confirmed"
            });
    }

    private sealed record ChangeProposalNotification(
        Guid OrderId,
        Guid UserId,
        Guid ProposalId,
        Guid CurrentDishId,
        string CurrentDishName,
        Guid? SuggestedDishId,
        string? SuggestedDishName,
        bool IsRequiredItem,
        Guid? RequiredCategoryId,
        IReadOnlyCollection<string> AllowedActions);

    private sealed record AutoOrderRefundPolicy(
        string Scope,
        string Name,
        decimal Percent,
        bool RequiresImage);

    private sealed record AutoFinalizeNotification(
        string TemplateKey,
        Guid UserId,
        Guid? ReferenceId,
        IReadOnlyDictionary<string, string> Tokens,
        object Data);
}
