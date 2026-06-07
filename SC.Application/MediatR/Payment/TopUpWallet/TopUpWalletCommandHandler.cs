using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Payment;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Payment.TopUpWallet;

internal class TopUpWalletCommandHandler(
    IPaymentService paymentService,
    ILogger<TopUpWalletCommandHandler> logger)
    : ICommandHandler<TopUpWalletCommand, TopUpWalletResponse>
{
    public async Task<Result<TopUpWalletResponse>> Handle(
        TopUpWalletCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await paymentService.TopUpWalletAsync(
                request.AmountVnd,
                request.Method,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<TopUpWalletResponse>(
                    result.Error ?? Error.ServerError,
                    result.Message);
            }

            var value = result.Value!;
            var response = new TopUpWalletResponse
            {
                PaymentId = value.PaymentId,
                AmountVnd = value.AmountVnd,
                ConvertedPoints = value.ConvertedPoints,
                Method = value.Method,
                Status = value.Status,
                GatewayOrderId = value.GatewayOrderId,
                PaymentContent = value.PaymentContent,
                PayUrl = value.PayUrl
            };

            return Result.Success(response, result.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling top-up wallet command");
            return Result.Failure<TopUpWalletResponse>(
                Error.ServerError,
                "An error occurred while topping up wallet.");
        }
    }
}
