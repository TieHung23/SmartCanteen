using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.ChangeProposal;

public class AcceptChangeProposalCommand : ICommand<AcceptChangeProposalResponse>
{
    public Guid ProposalId { get; set; }
    public Guid NewDishId { get; set; }
}
