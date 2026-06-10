using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Refund.SubmitRefundRequest;

public sealed class SubmitRefundRequestCommand : ICommand<SubmitRefundRequestResponse>
{
    public Guid OrderId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<RefundImageInput> Images { get; set; } = [];
}
