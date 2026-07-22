using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal static class ChangeProposalMapping
{
    public static ChangeProposalResponse ToResponse(
        OrderItemChangeProposal proposal,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishes)
    {
        dishes.TryGetValue(proposal.CurrentDishId, out var currentDish);
        DishAggregateRoot? suggestedDish = null;
        DishAggregateRoot? selectedDish = null;

        if (proposal.SuggestedDishId.HasValue)
            dishes.TryGetValue(proposal.SuggestedDishId.Value, out suggestedDish);

        if (proposal.SelectedDishId.HasValue)
            dishes.TryGetValue(proposal.SelectedDishId.Value, out selectedDish);

        return new ChangeProposalResponse
        {
            Id = proposal.Id,
            OrderId = proposal.OrderId,
            CurrentDishId = proposal.CurrentDishId,
            CurrentDishName = currentDish?.Name ?? string.Empty,
            SuggestedDishId = proposal.SuggestedDishId,
            SuggestedDishName = suggestedDish?.Name,
            SelectedDishId = proposal.SelectedDishId,
            SelectedDishName = selectedDish?.Name,
            IsRequiredItem = proposal.IsRequiredItem,
            RequiredCategoryId = proposal.RequiredCategoryId,
            ProposalStatus = (int)proposal.ProposalStatus,
            AllowedActions = GetAllowedActions(proposal),
            RespondedAtUtc = proposal.RespondedAtUtc,
            CreatedAtUtc = proposal.CreatedAtUtc
        };
    }

    private static List<string> GetAllowedActions(OrderItemChangeProposal proposal)
    {
        if (proposal.ProposalStatus != ChangeProposalStatus.WaitingResponse)
            return [];

        return proposal.IsRequiredItem
            ? ["SwapItem", "RefundOrder"]
            : ["SwapItem", "RefundItem", "RefundOrder"];
    }
}
