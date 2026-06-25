using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Payment.GetTopUpPolicy;

public sealed record GetTopUpPolicyQuery : IQuery<GetTopUpPolicyResponse>;
