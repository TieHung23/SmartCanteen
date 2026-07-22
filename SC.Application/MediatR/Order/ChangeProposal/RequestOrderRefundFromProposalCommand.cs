using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.ChangeProposal;

public class RequestOrderRefundFromProposalCommand : ICommand<RequestOrderRefundFromProposalResponse>
{
    public Guid ProposalId { get; set; }
}
