using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Order.DeleteOrder;

internal class DeleteOrderCommandHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteOrderCommandHandler> logger
) : ICommandHandler<DeleteOrderCommand, DeleteOrderResponse>
{
    public async Task<Result<DeleteOrderResponse>> Handle(
        DeleteOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);
            if (order is null)
            {
                return Result.Failure<DeleteOrderResponse>(
                    Error.NullValue,
                    $"Order with id {request.Id} not found.");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            order.SoftDelete();
            orderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new DeleteOrderResponse
            {
                Id = request.Id,
                Message = "Order deleted successfully (soft delete)."
            };

            return Result.Success(response, "Order deleted successfully.");
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error deleting order with id {OrderId}", request.Id);
            return Result.Failure<DeleteOrderResponse>(
                Error.ServerError,
                "An error occurred while deleting the order.");
        }
    }
}
