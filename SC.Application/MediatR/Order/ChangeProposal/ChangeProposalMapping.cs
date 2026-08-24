using SC.Domain.Domain.Order.AggregateRoot;
using SC.Domain.Domain.Order.Enum;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal static class ChangeProposalMapping
{
    public static ChangeProposalResponse ToResponse(
        OrderItemChangeProposal proposal,
        IReadOnlyDictionary<Guid, DishAggregateRoot> dishes,
        decimal? currentUnitPrice = null)
    {
        dishes.TryGetValue(proposal.CurrentDishId, out var currentDish);
        DishAggregateRoot? suggestedDish = null;
        DishAggregateRoot? selectedDish = null;

        if (proposal.SuggestedDishId.HasValue)
            dishes.TryGetValue(proposal.SuggestedDishId.Value, out suggestedDish);

        if (proposal.SelectedDishId.HasValue)
            dishes.TryGetValue(proposal.SelectedDishId.Value, out selectedDish);

        var nowUtc = DateTimeOffset.UtcNow;

        return new ChangeProposalResponse
        {
            Id = proposal.Id,
            OrderId = proposal.OrderId,
            CurrentDishId = proposal.CurrentDishId,
            CurrentDishName = currentDish?.Name ?? string.Empty,
            CurrentUnitPrice = currentUnitPrice,
            SuggestedDishId = proposal.SuggestedDishId,
            SuggestedDishName = suggestedDish?.Name,
            SelectedDishId = proposal.SelectedDishId,
            SelectedDishName = selectedDish?.Name,
            IsRequiredItem = proposal.IsRequiredItem,
            RequiredCategoryId = proposal.RequiredCategoryId,
            ProposalStatus = (int)proposal.ProposalStatus,
            AllowedActions = GetAllowedActions(proposal, nowUtc),
            ExpiresAtUtc = proposal.ExpiresAtUtc,
            IsExpired = proposal.IsExpired(nowUtc),
            RespondedAtUtc = proposal.RespondedAtUtc,
            CreatedAtUtc = proposal.CreatedAtUtc
        };
    }

    private static List<string> GetAllowedActions(
        OrderItemChangeProposal proposal,
        DateTimeOffset nowUtc)
    {
        if (proposal.ProposalStatus != ChangeProposalStatus.WaitingResponse
            || proposal.IsExpired(nowUtc))
        {
            return [];
        }

        return proposal.IsRequiredItem
            ? ["SwapItem", "RefundOrder"]
            : ["SwapItem", "RefundItem", "RefundOrder"];
    }
}
