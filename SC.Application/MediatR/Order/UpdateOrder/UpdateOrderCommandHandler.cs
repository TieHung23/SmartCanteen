using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Contract.Services.Notification;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Order.UpdateOrder;

internal class UpdateOrderCommandHandler(
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    IBusinessNotificationService businessNotificationService,
    IOrderStatusNotifier orderStatusNotifier,
    ILogger<UpdateOrderCommandHandler> logger
) : ICommandHandler<UpdateOrderCommand, UpdateOrderResponse>
{
    public async Task<Result<UpdateOrderResponse>> Handle(
        UpdateOrderCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(request.Id, cancellationToken);

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

            if (order.Status == newStatus)
            {
                return Result.Success(
                    new UpdateOrderResponse
                    {
                        Id = order.Id,
                        Status = (int)newStatus,
                        Message = $"Order is already {newStatus}."
                    },
                    "Order status was unchanged.");
            }

            // CHOT DONG BO: 2 trang thai nay do LUONG PHUC VU tu cap nhat (robot rap -> Preparing;
            //   staff quet khay+o assign -> ReadyForPickup). KHONG cho set THU CONG qua UpdateOrder
            //   -> tranh lech voi ServingJob (vd don b488feee: set ReadyForPickup tay khi ke con trong).
            //   (Completed van cho: staff phuc vu thu cong khi robot loi. Cancelled/Expired: admin xu ly.)
            if (newStatus is OrderStatus.ReadyForPickup or OrderStatus.Preparing)
            {
                return Result.Failure<UpdateOrderResponse>(
                    Error.InvalidValue,
                    $"Trạng thái '{newStatus}' do luồng phục vụ tự cập nhật (robot ráp / staff quét khay lên kệ), " +
                    "không được set thủ công. Muốn đưa đơn lên kệ hãy dùng quét khay + ô (assign).");
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var fromStatus = order.Status;
            order.UpdateStatus(newStatus, currentUserId);
            orderRepository.Update(order);
            await orderStatusHistoryRepository.AddAsync(
                OrderStatusHistoryEntity.Create(order.Id, fromStatus, newStatus, currentUserId, "ManualUpdate"),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            await businessNotificationService.NotifyAsync(
                NotificationTemplateKeys.OrderStatusChanged,
                order.CreatedBy,
                order.Id,
                new Dictionary<string, string>
                {
                    ["referenceId"] = order.Id.ToString(),
                    ["status"] = newStatus.ToString()
                },
                new
                {
                    OrderId = order.Id,
                    Status = newStatus.ToString()
                },
                cancellationToken);

            // Bắn real-time đổi status cho Học Sinh + Staff (FE cập nhật badge live).
            await orderStatusNotifier.BroadcastAsync(
                order.Id, order.CreatedBy, (int)newStatus, newStatus.ToString(), cancellationToken);

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
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error updating order with id {OrderId}", request.Id);
            return Result.Failure<UpdateOrderResponse>(
                Error.ServerError,
                "An error occurred while updating the order.");
        }
    }
}
