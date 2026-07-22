namespace SC.Application.MediatR.Order.ChangeProposal;

public class RequestRefundFromProposalResponse
{
    public Guid RefundRequestId { get; set; }
    public Guid OrderId { get; set; }
    public int OrderItemId { get; set; }
    public Guid DishId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
