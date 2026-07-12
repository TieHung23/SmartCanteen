using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Robot.PullNextJob;

internal sealed class PullNextJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<PullNextJobCommandHandler> logger
) : ICommandHandler<PullNextJobCommand, PullNextJobResponse>
{
    public async Task<Result<PullNextJobResponse>> Handle(
        PullNextJobCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            // 1) Job Queued cũ nhất (FIFO theo giờ tạo). Chưa có việc -> Job = null (204).
            var queuedJobs = await servingJobRepository
                .FindListAsync(x => x.Status == ServingJobStatus.Queued, cancellationToken);
            var job = queuedJobs.OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
            if (job is null)
            {
                return Result.Success(new PullNextJobResponse(null), "No queued job.");
            }

            // 2) Khay: job REQUEUE còn giữ khay cũ (món đã gắp nằm trên đó) -> TÁI DÙNG.
            //    Job mới -> gán khay TRỐNG (lazy). Hết khay -> giữ Queued, trả null.
            TrayEntity? tray = null;
            if (job.TrayId is Guid heldTrayId)
            {
                var held = await trayRepository.GetByIdAsync(heldTrayId, cancellationToken);
                if (held is not null && held.CurrentOrderId == job.OrderId)
                    tray = held;                       // khay vẫn thuộc đơn này -> dùng tiếp
            }
            if (tray is null)
            {
                var availableTrays = await trayRepository
                    .FindListAsync(x => x.Status == TrayStatus.Available, cancellationToken);
                tray = availableTrays.OrderBy(x => x.CreatedAtUtc).FirstOrDefault();
                if (tray is null)
                {
                    return Result.Success(new PullNextJobResponse(null), "No free tray.");
                }
            }

            // 3) Lấy order + items
            var order = await orderRepository.GetByIdAsync(
                job.OrderId, cancellationToken, o => o.OrderItems);
            if (order is null)
            {
                job.Cancel(actorId);                      // order biến mất -> huỷ job, bỏ qua
                servingJobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(new PullNextJobResponse(null), "Order missing; job cancelled.");
            }

            // 4) Gán khay (Available->Reserved) + claim job (Queued->Pushed). 1 SaveChanges = atomic.
            //     Nhiều robot: cần optimistic-concurrency / SELECT FOR UPDATE để không claim trùng.
            //       Hiện 1 service nên an toàn; nâng khi chạy nhiều tay.
            tray.Reserve(order.Id, actorId);
            trayRepository.Update(tray);
            job.AssignTray(tray.Id, actorId);
            job.MarkPushed(actorId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var message = new ServingJobMessage
            {
                JobId = job.Id,
                OrderId = order.Id,
                TrayId = tray.Id,
                TrayCode = tray.Code,
                Items = order.OrderItems
                    .Select(i => new ServingJobItemMessage { DishId = i.DishId, Quantity = i.Quantity })
                    .ToList()
            };

            return Result.Success(new PullNextJobResponse(message), "Job dispatched.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pulling next serving job");
            return Result.Failure<PullNextJobResponse>(
                Error.ServerError, "An error occurred while pulling the next job.");
        }
    }
}
