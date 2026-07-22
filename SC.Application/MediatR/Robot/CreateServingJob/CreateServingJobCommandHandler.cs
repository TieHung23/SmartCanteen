using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Order.Enum;
using SC.Domain.Domain.ServingJob.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using OrderStatusHistoryEntity = SC.Domain.Domain.OrderStatusHistory.Entity.OrderStatusHistory;

namespace SC.Application.MediatR.Robot.CreateServingJob;

internal sealed class CreateServingJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
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
            var existingJobs = await servingJobRepository
                .FindListAsync(x => x.OrderId == request.OrderId
                                   && x.Status != ServingJobStatus.Cancelled
                                   && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
            var existing = existingJobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();

            if (existing is not null)
            {
                return Result.Success(
                    new CreateServingJobResponse(existing.Id, existing.OrderId, existing.TrayId, existing.Status.ToString()),
                    "Serving job already exists for this order.");
            }

            // HYBRID: KHÔNG gán khay ở đây. Tạo job Queued; khay gán LAZY khi robot pull next-job.
            var job = ServingJobEntity.Create(order.Id, currentUserId);
            await servingJobRepository.AddAsync(job, cancellationToken);

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

            // HYBRID: chỉ PING đánh thức robot (không kèm data) -> robot tự pull next-job.
            // best-effort: ping lỗi KHÔNG rollback job đã lưu.
            try
            {
                await servingJobNotifier.PingNewJobAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ping robots for serving job {JobId} (order {OrderId})", job.Id, order.Id);
            }

            return Result.Success(
                new CreateServingJobResponse(job.Id, job.OrderId, job.TrayId, job.Status.ToString()),
                "Serving job queued; robots pinged.");
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
