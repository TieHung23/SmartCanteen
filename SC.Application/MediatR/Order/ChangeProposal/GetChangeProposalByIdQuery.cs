using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.ChangeProposal;

public class GetChangeProposalByIdQuery : IQuery<ChangeProposalResponse>
{
    public Guid ProposalId { get; set; }
}
