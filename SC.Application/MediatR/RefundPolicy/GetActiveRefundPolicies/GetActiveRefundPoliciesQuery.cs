using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RefundPolicy.GetActiveRefundPolicies;

public sealed record GetActiveRefundPoliciesQuery
    : IQuery<IReadOnlyList<GetActiveRefundPoliciesResponse>>;
