using SC.Contract.Shared;

namespace SC.Contract.Services.Payment;

public record TopUpWalletResult(
    Guid PaymentId,
    decimal AmountVnd,
    decimal ConvertedPoints,
    int Method,
    string Status,
    string GatewayOrderId,
    string PaymentContent,
    string? PayUrl);

public record CompletePaymentResult(
    Guid PaymentId,
    string GatewayOrderId,
    string Status,
    decimal ConvertedPoints);

public interface IPaymentService
{
    Task<Result<TopUpWalletResult>> TopUpWalletAsync(
        decimal amountVnd,
        int method,
        CancellationToken cancellationToken = default);

    Task<Result<CompletePaymentResult>> HandleSepayIpnAsync(
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);
}
