namespace SC.Application.MediatR.Payment.TopUpWallet;

public class TopUpWalletResponse
{
    public Guid PaymentId { get; set; }
    public decimal AmountVnd { get; set; }
    public decimal ConvertedPoints { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int Method { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string PaymentContent { get; set; } = string.Empty;
    public string? PayUrl { get; set; }
}
