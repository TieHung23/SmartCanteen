using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Payment;
using SC.Contract.Services.Notification;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Payment.HandleSepayIpn;

internal class HandleSepayIpnCommandHandler(
    IPaymentService paymentService,
    IBusinessNotificationService businessNotificationService,
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
            if (!value.WasAlreadyCompleted)
            {
                await businessNotificationService.NotifyAsync(
                    NotificationTemplateKeys.PaymentCompleted,
                    value.UserId,
                    value.PaymentId,
                    new Dictionary<string, string>
                    {
                        ["referenceId"] = value.PaymentId.ToString(),
                        ["points"] = value.ConvertedPoints.ToString("0.##")
                    },
                    new
                    {
                        value.PaymentId,
                        value.ConvertedPoints,
                        value.Status
                    },
                    cancellationToken);
            }

            return Result.Success(
                new HandleSepayIpnResponse
                {
                    PaymentId = value.PaymentId,
                    GatewayOrderId = value.GatewayOrderId,
                    Status = value.Status,
                    ConvertedPoints = value.ConvertedPoints
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
