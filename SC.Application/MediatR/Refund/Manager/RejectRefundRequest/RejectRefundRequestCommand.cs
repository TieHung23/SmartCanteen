using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Refund.Manager.RejectRefundRequest;

public sealed record RejectRefundRequestCommand(Guid Id, string Reason)
    : ICommand<RejectRefundRequestResponse>;

public sealed class RejectRefundRequestResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}
