using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Refund.Manager.ApproveRefundRequest;

public sealed record ApproveRefundRequestCommand(Guid Id)
    : ICommand<ApproveRefundRequestResponse>;

public sealed class ApproveRefundRequestResponse
{
    public Guid Id { get; set; }
    public Guid WalletTransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = string.Empty;
}
