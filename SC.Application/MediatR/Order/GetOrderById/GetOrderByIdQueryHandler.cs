using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.GetOrderById;

internal class GetOrderByIdQueryHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
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

            var dishIds = order.OrderItems.Select(i => i.DishId).Distinct().ToList();
            var dishes = await dishRepository
                .FindListAsync(d => dishIds.Contains(d.Id), cancellationToken);
            var dishMap = dishes.ToDictionary(d => d.Id);

            var totalPrice = order.OrderItems.Sum(item => item.UnitPrice.Amount * item.Quantity);

            var response = new GetOrderByIdResponse
            {
                Id = order.Id,
                SessionId = order.SessionId,
                MealTemplateId = order.MealTemplateId,
                TransactionId = order.WalletTransactionId,
                UserId = order.CreatedBy,
                Status = (int)order.Status,
                TotalPrice = totalPrice,
                Items = order.OrderItems.Select(item =>
                {
                    var dish = dishMap.GetValueOrDefault(item.DishId);
                    return new OrderItemDto
                    {
                        DishId = item.DishId,
                        DishName = dish?.Name ?? string.Empty,
                        ImgUrl = dish?.ImgUrl,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice.Amount
                    };
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
