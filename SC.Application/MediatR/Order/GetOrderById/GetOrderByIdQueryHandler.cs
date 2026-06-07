using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.GetOrderById;

internal class GetOrderByIdQueryHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ILogger<GetOrderByIdQueryHandler> logger
) : IQueryHandler<GetOrderByIdQuery, GetOrderByIdResponse>
{
    public async Task<Result<GetOrderByIdResponse>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

            if (order is null)
            {
                return Result.Failure<GetOrderByIdResponse>(
                    Error.NullValue,
                    $"Order with id {request.Id} not found.");
            }

            var totalPrice = order.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);

            var response = new GetOrderByIdResponse
            {
                Id = order.Id,
                MealId = order.MealId,
                TransactionId = order.WalletTransactionId,
                UserId = order.CreatedBy,
                Status = (int)order.Status,
                TotalPrice = totalPrice,
                Items = order.OrderItems.Select(item => new OrderItemDto
                {
                    DishId = item.DishId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice.Amount
                }).ToList(),
                CreatedAtUtc = order.CreatedAtUtc,
                UpdatedAtUtc = order.UpdatedAtUtc
            };

            return Result.Success(response, "Order retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order with id {OrderId}", request.Id);
            return Result.Failure<GetOrderByIdResponse>(
                Error.ServerError,
                "An error occurred while retrieving the order.");
        }
    }
}
