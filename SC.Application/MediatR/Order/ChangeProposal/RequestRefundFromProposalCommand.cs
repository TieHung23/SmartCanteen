using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.ChangeProposal;

public class RequestRefundFromProposalCommand : ICommand<RequestRefundFromProposalResponse>
{
    public Guid ProposalId { get; set; }
}
