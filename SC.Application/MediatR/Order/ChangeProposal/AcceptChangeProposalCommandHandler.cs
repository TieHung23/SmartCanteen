using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using DishEntity = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class AcceptChangeProposalCommandHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<DishEntity, Guid> dishRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : ICommandHandler<AcceptChangeProposalCommand, AcceptChangeProposalResponse>
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

        var newDish = await dishRepository.GetByIdAsync(request.NewDishId, cancellationToken);
        if (newDish is null || newDish.IsDeleted || !newDish.IsActive)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "New dish not found or inactive.");

        var order = await orderRepository.FindSingleAsync(
            o => o.Id == proposal.OrderId,
            cancellationToken);

        if (order is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Order not found.");

        var item = order.OrderItems.FirstOrDefault(i => i.DishId == proposal.CurrentDishId);
        if (item is null)
            return Result.Failure<AcceptChangeProposalResponse>(Error.NullValue, "Order item not found.");

        proposal.Accept(currentUserService.UserId);
        item.SwapDish(request.NewDishId, newDish.Price.Amount);

        proposalRepository.Update(proposal);
        orderRepository.Update(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new AcceptChangeProposalResponse
        {
            Message = "Dish swapped successfully."
        };

        return Result.Success(response, "Dish swapped successfully.");
    }
}
