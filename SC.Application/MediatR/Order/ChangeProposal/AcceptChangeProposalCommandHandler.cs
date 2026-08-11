using Microsoft.EntityFrameworkCore;
using SC.Application.MediatR.Cart;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using SessionAggregateRoot = SC.Domain.Domain.Session.AggregateRoot.Session;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using DishEntity = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class AcceptChangeProposalCommandHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<DishEntity, Guid> dishRepository,
    IGenericRepository<SessionAggregateRoot, Guid> sessionRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService) : ICommandHandler<AcceptChangeProposalCommand, AcceptChangeProposalResponse>
{
    public async Task<Result<AcceptChangeProposalResponse>> Handle(AcceptChangeProposalCommand request, CancellationToken cancellationToken)
    {
        var proposal = await proposalRepository.FindSingleAsync(
            p => p.Id == request.ProposalId,
            cancellationToken);

        if (proposal is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Proposal not found.");

        if (proposal.UserId != currentUserService.UserId)
            return Result.Failure<AcceptChangeProposalResponse>(Error.InvalidValue, "This proposal does not belong to you.");

        if (proposal.IsExpired(DateTimeOffset.UtcNow))
        {
            return Result.Failure<AcceptChangeProposalResponse>(
                Error.InvalidValue,
                "Change proposal has expired.");
        }

        var newDish = await dishRepository.GetByIdAsync(request.NewDishId, cancellationToken);
        if (newDish is null || newDish.IsDeleted || !newDish.IsActive)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "New dish not found or inactive.");

        var currentDish = await dishRepository.GetByIdAsync(proposal.CurrentDishId, cancellationToken);
        if (currentDish is null || currentDish.IsDeleted)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Current dish not found.");

        if (proposal.IsRequiredItem
            && proposal.RequiredCategoryId.HasValue
            && newDish.CategoryId != proposal.RequiredCategoryId.Value)
        {
            return Result.Failure<AcceptChangeProposalResponse>(
                Error.InvalidValue,
                "Required item must be swapped with a dish from the same required category.");
        }

        if (!proposal.IsRequiredItem && newDish.CategoryId != currentDish.CategoryId)
        {
            return Result.Failure<AcceptChangeProposalResponse>(
                Error.InvalidValue,
                "Optional item must be swapped with a dish from the same category.");
        }

        var order = await orderRepository.FindSingleAsync(
            o => o.Id == proposal.OrderId && !o.IsDeleted,
            cancellationToken);

        if (order is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Order not found.");

        if (order.Status != OrderStatus.Preparing)
        {
            return Result.Failure<AcceptChangeProposalResponse>(
                Error.InvalidValue,
                "Order is no longer available for change proposal actions.");
        }

        var session = await sessionRepository.FindSingleAsync(
            s => s.Id == order.SessionId && !s.IsDeleted,
            q => q.Include(s => s.SessionDishes)
                .Include(s => s.MealTemplates)
                .ThenInclude(t => t.Settings),
            cancellationToken);

        if (session is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Session not found.");

        if (session.SessionDishes.All(d => d.DishId != request.NewDishId))
        {
            return Result.Failure<AcceptChangeProposalResponse>(
                Error.InvalidValue,
                "New dish is not part of this session.");
        }

        var item = order.OrderItems.FirstOrDefault(i => i.DishId == proposal.CurrentDishId);
        if (item is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Order item not found.");

        var template = session.MealTemplates.FirstOrDefault(t =>
            t.Id == order.MealTemplateId && !t.IsDeleted);

        if (template is not null)
        {
            var validationDishIds = order.OrderItems
                .Where(i => i.ItemStatus != OrderItemStatus.Refunded
                            && (i.DishId == proposal.CurrentDishId
                                || i.ItemStatus != OrderItemStatus.ChangePending))
                .Select(i => i.DishId == proposal.CurrentDishId ? request.NewDishId : i.DishId)
                .Distinct()
                .ToList();

            var validationDishes = await dishRepository.FindListAsync(
                d => validationDishIds.Contains(d.Id),
                cancellationToken);
            var validationDishMap = validationDishes.ToDictionary(d => d.Id);

            if (validationDishIds.Any(id => !validationDishMap.ContainsKey(id)))
            {
                return Result.Failure<AcceptChangeProposalResponse>(
                    Error.InvalidValue,
                    "Order contains a dish that cannot be validated against the meal template.");
            }

            var validationItems = order.OrderItems
                .Where(i => i.ItemStatus != OrderItemStatus.Refunded
                            && (i.DishId == proposal.CurrentDishId
                                || i.ItemStatus != OrderItemStatus.ChangePending))
                .Select(i => new CartItemData
                {
                    DishId = i.DishId == proposal.CurrentDishId ? request.NewDishId : i.DishId,
                    Quantity = i.Quantity
                })
                .ToList();

            var templateValidation = CartTemplateRuleValidator.Validate(
                template,
                validationItems,
                validationDishMap,
                requireCompleteTemplate: true);

            if (templateValidation.IsFailure)
            {
                return Result.Failure<AcceptChangeProposalResponse>(
                    templateValidation.Error ?? Error.InvalidValue,
                    templateValidation.Message);
            }
        }

        proposal.Accept(request.NewDishId, currentUserService.UserId);
        item.SwapDish(request.NewDishId, newDish.Price.Amount);

        proposalRepository.Update(proposal);
        orderRepository.Update(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyManagerAsync(
            session.CreatedBy,
            order.Id,
            proposal.Id,
            currentUserService.UserId,
            request.NewDishId,
            cancellationToken);

        var response = new AcceptChangeProposalResponse
        {
            Message = "Dish swapped successfully."
        };

        return Result.Success(response, "Dish swapped successfully.");
    }

    private async Task NotifyManagerAsync(
        Guid managerId,
        Guid orderId,
        Guid proposalId,
        Guid userId,
        Guid selectedDishId,
        CancellationToken cancellationToken)
    {
        if (managerId == Guid.Empty || managerId == userId)
            return;

        await businessNotificationService.NotifyAsync(
            NotificationTemplateKeys.ChangeProposalAccepted,
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
                SelectedDishId = selectedDishId,
                Action = "SwapItem"
            },
            cancellationToken);
    }
}
