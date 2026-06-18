using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Robot.CreateServingJob;

internal sealed class CreateServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<OrderStatusHistoryEntity, Guid> orderStatusHistoryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingJobNotifier servingJobNotifier,
    ILogger<CreateServingJobCommandHandler> logger
) : ICommandHandler<CreateServingJobCommand, CreateServingJobResponse>
{
    public async Task<Result<CreateServingJobResponse>> Handle(
        CreateServingJobCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        try
        {
            var order = await orderRepository.GetByIdAsync(
                request.OrderId, cancellationToken, o => o.OrderItems);

            if (order is null)
            {
                return Result.Failure<CreateServingJobResponse>(
                    Error.OrderNotFound, "Order was not found.");
            }

            // Idempotent: order đã có job đang hoạt động -> trả lại
            var existing = await servingJobRepository
                .GetQueryable(x => x.OrderId == request.OrderId
                                   && x.Status != ServingJobStatus.Cancelled
                                   && x.Status != ServingJobStatus.Collected)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return Result.Success(
                    new CreateServingJobResponse(existing.Id, existing.OrderId, existing.TrayId, existing.Status.ToString()),
                    "Serving job already exists for this order.");
            }

            // Gán khay: ưu tiên trayId chỉ định, nếu không lấy 1 khay Available trong pool
            TrayEntity? tray;
            if (request.TrayId is { } trayId)
            {
                tray = await trayRepository.GetByIdAsync(trayId, cancellationToken);
            }
            else
            {
                tray = await trayRepository
                    .GetQueryable(x => x.Status == TrayStatus.Available)
                    .OrderBy(x => x.CreatedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var job = ServingJobEntity.Create(order.Id, currentUserId, tray?.Id);
            job.MarkPushed(null, currentUserId);
            await servingJobRepository.AddAsync(job, cancellationToken);

            if (tray is not null)
            {
                tray.Reserve(order.Id, currentUserId);
                trayRepository.Update(tray);
            }

            // Order -> Preparing + ghi lịch sử
            var fromStatus = order.Status;
            if (order.Status != OrderStatus.Preparing)
            {
                order.UpdateStatus(OrderStatus.Preparing, currentUserId);
                orderRepository.Update(order);
                await orderStatusHistoryRepository.AddAsync(
                    OrderStatusHistoryEntity.Create(order.Id, fromStatus, OrderStatus.Preparing, currentUserId, "ServingJobCreated"),
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Đẩy cho robot service (best-effort: lỗi push không rollback job đã lưu)
            try
            {
                var message = new ServingJobMessage
                {
                    JobId = job.Id,
                    OrderId = order.Id,
                    TrayId = tray?.Id,
                    TrayCode = tray?.Code,
                    Items = order.OrderItems
                        .Select(i => new ServingJobItemMessage { DishId = i.DishId, Quantity = i.Quantity })
                        .ToList()
                };
                await servingJobNotifier.PushJobAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to push serving job {JobId} for order {OrderId}", job.Id, order.Id);
            }

            return Result.Success(
                new CreateServingJobResponse(job.Id, job.OrderId, job.TrayId, job.Status.ToString()),
                "Serving job created and pushed to robot service.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating serving job for order {OrderId}", request.OrderId);
            return Result.Failure<CreateServingJobResponse>(
                Error.ServerError,
                "An error occurred while creating the serving job.");
        }
    }
}
