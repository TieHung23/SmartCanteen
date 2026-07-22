using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Order.ChangeProposal;

public class GetMyChangeProposalsQuery : IQuery<List<ChangeProposalResponse>>
{
}
