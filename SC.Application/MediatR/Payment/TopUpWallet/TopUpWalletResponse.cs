namespace SC.Application.MediatR.Payment.TopUpWallet;

public class TopUpWalletResponse
{
    public Guid PaymentId { get; set; }
    public decimal AmountVnd { get; set; }
    public decimal ConvertedPoints { get; set; }
    public int Method { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string PaymentContent { get; set; } = string.Empty;
    public string? PayUrl { get; set; }
    public string? QrCodeUrl { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
}
