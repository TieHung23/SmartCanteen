using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class GetMyChangeProposalsQueryHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
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

        var response = proposals
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => ChangeProposalMapping.ToResponse(p, dishMap))
            .ToList();

        return Result.Success(response, "Change proposals retrieved successfully.");
    }
}
