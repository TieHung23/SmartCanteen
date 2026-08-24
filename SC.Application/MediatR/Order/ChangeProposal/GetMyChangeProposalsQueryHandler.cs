using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class GetMyChangeProposalsQueryHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ICurrentUserService currentUserService) : IQueryHandler<GetMyChangeProposalsQuery, List<ChangeProposalResponse>>
{
    public async Task<Result<List<ChangeProposalResponse>>> Handle(
        GetMyChangeProposalsQuery request,
        CancellationToken cancellationToken)
    {
        var proposals = await proposalRepository.FindListAsync(
            p => p.UserId == currentUserService.UserId,
            cancellationToken);

        var dishIds = proposals
            .SelectMany(p => new[] { p.CurrentDishId, p.SuggestedDishId, p.SelectedDishId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var dishes = await dishRepository.FindListAsync(
            d => dishIds.Contains(d.Id),
            cancellationToken);
        var dishMap = dishes.ToDictionary(d => d.Id);

        // Charged price per (order, dish), so each proposal can carry the exact amount a
        // replacement has to match - see AcceptChangeProposalCommandHandler.
        var orderIds = proposals
            .Select(p => p.OrderId)
            .Distinct()
            .ToList();

        var orders = await orderRepository.FindListAsync(
            o => orderIds.Contains(o.Id) && !o.IsDeleted,
            cancellationToken);

        var unitPriceByOrderDish = orders
            .SelectMany(o => o.OrderItems.Select(i => new
            {
                Key = (OrderId: o.Id, i.DishId),
                i.UnitPrice.Amount
            }))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().Amount);

        var response = proposals
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => ChangeProposalMapping.ToResponse(
                p,
                dishMap,
                unitPriceByOrderDish.TryGetValue((p.OrderId, p.CurrentDishId), out var unitPrice)
                    ? unitPrice
                    : null))
            .ToList();

        return Result.Success(response, "Change proposals retrieved successfully.");
    }
}
