using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class GetChangeProposalByIdQueryHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ICurrentUserService currentUserService) : IQueryHandler<GetChangeProposalByIdQuery, ChangeProposalResponse>
{
    public async Task<Result<ChangeProposalResponse>> Handle(
        GetChangeProposalByIdQuery request,
        CancellationToken cancellationToken)
    {
        var proposal = await proposalRepository.FindSingleAsync(
            p => p.Id == request.ProposalId,
            cancellationToken);

        if (proposal is null)
            return Result.Failure<ChangeProposalResponse>(Error.NullValue, "Proposal not found.");

        if (proposal.UserId != currentUserService.UserId)
            return Result.Failure<ChangeProposalResponse>(Error.Forbidden, "This proposal does not belong to you.");

        var dishIds = new[] { proposal.CurrentDishId, proposal.SuggestedDishId, proposal.SelectedDishId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var dishes = await dishRepository.FindListAsync(
            d => dishIds.Contains(d.Id),
            cancellationToken);
        var dishMap = dishes.ToDictionary(d => d.Id);

        var response = ChangeProposalMapping.ToResponse(proposal, dishMap);
        return Result.Success(response, "Change proposal retrieved successfully.");
    }
}
