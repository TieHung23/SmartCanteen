using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.UpdateOrder;

internal class UpdateOrderCommandHandler(
    IRepositoryBase<OrderAggregateRoot, Guid> orderRepository,
    ICurrentUserService currentUserService,
    ILogger<UpdateOrderCommandHandler> logger
) : ICommandHandler<UpdateOrderCommand, UpdateOrderResponse>
{
    public async Task<Result<UpdateOrderResponse>> Handle(
        UpdateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderRepository.FindByIdAsync(request.Id, cancellationToken);

            if (order is null)
            {
                return Result.Failure<UpdateOrderResponse>(
                    Error.NullValue,
                    $"Order with id {request.Id} not found.");
            }

            // Validate status
            if (!Enum.IsDefined(typeof(OrderStatus), request.Status))
            {
                return Result.Failure<UpdateOrderResponse>(
                    Error.InvalidValue,
                    "Invalid order status.");
            }

            var currentUserId = currentUserService.UserId;
            var newStatus = (OrderStatus)request.Status;

            order.UpdateStatus(newStatus, currentUserId);

            var updateResult = await orderRepository.UpdateAsync(order);
            if (updateResult.IsFailure)
            {
                return Result.Failure<UpdateOrderResponse>(
                    updateResult.Error ?? Error.ServerError,
                    updateResult.Message);
            }

            var response = new UpdateOrderResponse
            {
                Id = order.Id,
                Status = (int)newStatus,
                Message = $"Order status updated to {newStatus}."
            };

            return Result.Success(response, "Order updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order with id {OrderId}", request.Id);
            return Result.Failure<UpdateOrderResponse>(
                Error.ServerError,
                "An error occurred while updating the order.");
        }
    }
}
