using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.AggregateRoot;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.ChangeProposal;

internal class RequestRefundFromProposalCommandHandler(
    IGenericRepository<OrderItemChangeProposal, Guid> proposalRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : ICommandHandler<RequestRefundFromProposalCommand, RequestRefundFromProposalResponse>
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

        var order = await orderRepository.FindSingleAsync(
            o => o.Id == proposal.OrderId,
            cancellationToken);

        if (order is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Order not found.");

        var item = order.OrderItems.FirstOrDefault(i => i.DishId == proposal.CurrentDishId);
        if (item is null)
            return Result.Failure<RequestRefundFromProposalResponse>(Error.NullValue, "Order item not found.");

        proposal.RequestRefund(currentUserService.UserId);
        item.RefundItem();

        proposalRepository.Update(proposal);
        orderRepository.Update(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RequestRefundFromProposalResponse
        {
            Message = "Refund requested. Please submit a refund request through the refund workflow."
        };

        return Result.Success(response, "Refund requested. Please submit a refund request through the refund workflow.");
    }
}
