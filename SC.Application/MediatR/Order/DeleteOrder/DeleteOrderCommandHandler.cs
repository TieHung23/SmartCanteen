using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.DeleteOrder;

internal class DeleteOrderCommandHandler(
    IRepositoryBase<OrderAggregateRoot, Guid> orderRepository,
    ILogger<DeleteOrderCommandHandler> logger
) : ICommandHandler<DeleteOrderCommand, DeleteOrderResponse>
{
    public async Task<Result<DeleteOrderResponse>> Handle(
        DeleteOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Soft delete order by ID
            var deleteResult = await orderRepository.SoftDeleteWithConditionAsync(
                o => o.Id == request.Id);

            if (deleteResult.IsFailure)
            {
                return Result.Failure<DeleteOrderResponse>(
                    deleteResult.Error ?? Error.ServerError,
                    deleteResult.Message);
            }

            // Check if entity was found (SoftDeleteWithConditionAsync returns success even if count is 0)
            var order = await orderRepository.FindByIdAsync(request.Id, cancellationToken);
            if (order is null)
            {
                return Result.Failure<DeleteOrderResponse>(
                    Error.NullValue,
                    $"Order with id {request.Id} not found.");
            }

            var response = new DeleteOrderResponse
            {
                Id = request.Id,
                Message = "Order deleted successfully (soft delete)."
            };

            return Result.Success(response, "Order deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order with id {OrderId}", request.Id);
            return Result.Failure<DeleteOrderResponse>(
                Error.ServerError,
                "An error occurred while deleting the order.");
        }
    }
}
