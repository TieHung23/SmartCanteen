using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Payment;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Payment.HandleSepayIpn;

internal class HandleSepayIpnCommandHandler(
    IPaymentService paymentService,
    ILogger<HandleSepayIpnCommandHandler> logger)
    : ICommandHandler<HandleSepayIpnCommand, HandleSepayIpnResponse>
{
    public async Task<Result<HandleSepayIpnResponse>> Handle(
        HandleSepayIpnCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await paymentService.HandleSepayIpnAsync(
                request.Data,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<HandleSepayIpnResponse>(
                    result.Error ?? Error.ServerError,
                    result.Message);
            }

            var value = result.Value!;
            return Result.Success(
                new HandleSepayIpnResponse
                {
                    PaymentId = value.PaymentId,
                    GatewayOrderId = value.GatewayOrderId,
                    Status = value.Status,
                    ConvertedPoints = value.ConvertedPoints,
                    BalanceAfter = value.BalanceAfter
                },
                result.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling SePay IPN command");
            return Result.Failure<HandleSepayIpnResponse>(
                Error.ServerError,
                "An error occurred while handling SePay IPN.");
        }
    }
}
