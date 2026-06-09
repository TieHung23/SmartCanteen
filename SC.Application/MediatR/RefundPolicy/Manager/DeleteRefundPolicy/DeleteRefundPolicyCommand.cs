using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RefundPolicy.Manager.DeleteRefundPolicy;

public sealed record DeleteRefundPolicyCommand(string Code) : ICommand<string>;
