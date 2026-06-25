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
    string? PayUrl,
    string BankName,
    string BankAccountNumber,
    string BankAccountName);

public record CompletePaymentResult(
    Guid PaymentId,
    Guid UserId,
    string GatewayOrderId,
    string Status,
    decimal ConvertedPoints,
    bool WasAlreadyCompleted);

public record TopUpPolicyResult(
    decimal VndPerPoint,
    decimal MinTopUpAmount,
    decimal MaxTopUpAmount,
    string Currency,
    string PointName);

public record SePayWebhookVerificationResult(
    bool IsValid,
    string Message);

public interface ISePayWebhookVerifier
{
    SePayWebhookVerificationResult Verify(
        string rawBody,
        string signature,
        string timestamp);
}

public interface IPaymentService
{
    Task<Result<TopUpWalletResult>> TopUpWalletAsync(
        decimal amountVnd,
        int method,
        CancellationToken cancellationToken = default);

    Task<Result<TopUpPolicyResult>> GetTopUpPolicyAsync(
        CancellationToken cancellationToken = default);

    Task<Result<CompletePaymentResult>> HandleSepayIpnAsync(
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);
}
