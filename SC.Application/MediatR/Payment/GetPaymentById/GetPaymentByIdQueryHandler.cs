using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using PaymentAggregateRoot = SC.Domain.Domain.Payment.AggregateRoot.Payment;

namespace SC.Application.MediatR.Payment.GetPaymentById;

internal class GetPaymentByIdQueryHandler(
    IGenericRepository<PaymentAggregateRoot, Guid> paymentRepository,
    ICurrentUserService currentUserService,
    ILogger<GetPaymentByIdQueryHandler> logger)
    : IQueryHandler<GetPaymentByIdQuery, GetPaymentByIdResponse>
{
    public async Task<Result<GetPaymentByIdResponse>> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await paymentRepository.GetByIdAsync(request.Id, cancellationToken);

            if (payment is null || payment.UserId != currentUserService.UserId)
            {
                return Result.Failure<GetPaymentByIdResponse>(
                    Error.NullValue,
                    "Payment not found.");
            }

            var response = new GetPaymentByIdResponse
            {
                PaymentId = payment.Id,
                UserId = payment.UserId,
                GatewayOrderId = payment.GatewayOrderId,
                GatewayTransactionId = payment.GatewayTransactionId,
                AmountVnd = payment.AmountVnd,
                ConvertedPoints = payment.ConvertedPoints,
                BalanceBefore = payment.BalanceSnapshot.BalanceBefore,
                BalanceAfter = payment.BalanceSnapshot.BalanceAfter,
                Method = (int)payment.Method,
                Type = (int)payment.Type,
                Status = payment.Status.ToString(),
                FailureReason = payment.FailureReason,
                CreatedAtUtc = payment.CreatedAtUtc,
                CompletedAtUtc = payment.CompletedAtUtc
            };

            return Result.Success(response, "Payment retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment by id {PaymentId}", request.Id);
            return Result.Failure<GetPaymentByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving payment.");
        }
    }
}
