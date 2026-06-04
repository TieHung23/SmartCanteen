namespace SC.Application.MediatR.Payment.GetPaymentById;

public class GetPaymentByIdResponse
{
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string GatewayOrderId { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public decimal AmountVnd { get; set; }
    public decimal ConvertedPoints { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int Method { get; set; }
    public int Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
