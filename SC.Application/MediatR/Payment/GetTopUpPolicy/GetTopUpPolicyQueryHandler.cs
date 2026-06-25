using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Payment;
using SC.Contract.Shared;

namespace SC.Application.MediatR.Payment.GetTopUpPolicy;

internal sealed class GetTopUpPolicyQueryHandler(
    IPaymentService paymentService,
    ILogger<GetTopUpPolicyQueryHandler> logger)
    : IQueryHandler<GetTopUpPolicyQuery, GetTopUpPolicyResponse>
{
    public async Task<Result<GetTopUpPolicyResponse>> Handle(
        GetTopUpPolicyQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await paymentService.GetTopUpPolicyAsync(cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<GetTopUpPolicyResponse>(
                    result.Error ?? Error.ServerError,
                    result.Message);
            }

            var value = result.Value!;
            return Result.Success(
                new GetTopUpPolicyResponse
                {
                    VndPerPoint = value.VndPerPoint,
                    MinTopUpAmount = value.MinTopUpAmount,
                    MaxTopUpAmount = value.MaxTopUpAmount,
                    Currency = value.Currency,
                    PointName = value.PointName
                },
                result.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling get top-up policy query");
            return Result.Failure<GetTopUpPolicyResponse>(
                Error.ServerError,
                "An error occurred while retrieving top-up policy.");
        }
    }
}
