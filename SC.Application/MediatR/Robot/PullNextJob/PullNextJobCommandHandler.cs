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
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.Robot.PullNextJob;

internal sealed class PullNextJobCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
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
                // Khay còn bận (chưa Available) nghĩa là vẫn của job này: force-release đã gỡ
                // job.TrayId=null khi thu khay, nên job còn trỏ khay bận = khay chưa bị lấy đi.
                if (held is not null && held.Status != TrayStatus.Available)
                    tray = held;                       // dùng tiếp khay cũ (món đã gắp còn trên đó)
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

            // 4) Nhãn cho robot: món -> LaneCode (gắp Ở ĐÂU) + Station (TAY nào), theo cấu hình CA này.
            //    Đọc TRƯỚC khi claim job: query lỗi thì job không bị kẹt ở Pushed mà chẳng ai làm.
            //    Món chưa cấu hình lane -> LaneCode/Station = null (không fail pull; edge/staff xử lý).
            var dishIds = order.OrderItems.Select(i => i.DishId).Distinct().ToList();

            var configs = await slotConfigurationRepository.FindListAsync(
                x => x.SessionId == order.SessionId && !x.IsDeleted, cancellationToken);
            var configByDish = configs
                .GroupBy(x => x.DishId)
                .ToDictionary(g => g.Key, g => g.First());

            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNameById = dishes.ToDictionary(x => x.Id, x => x.Name);

            var armIds = configByDish.Values
                .Where(x => x.RobotArmId.HasValue)
                .Select(x => x.RobotArmId!.Value)
                .Distinct()
                .ToList();
            var arms = armIds.Count == 0
                ? new List<RobotArmEntity>()
                : await robotArmRepository.FindListAsync(x => armIds.Contains(x.Id), cancellationToken);
            var armCodeById = arms.ToDictionary(x => x.Id, x => x.Code);

            // 5) Gán khay (Available->Reserved) + claim job (Queued->Pushed). 1 SaveChanges = atomic.
            //     Nhiều robot: cần optimistic-concurrency / SELECT FOR UPDATE để không claim trùng.
            //       Hiện 1 service nên an toàn; nâng khi chạy nhiều tay.
            tray.Reserve(actorId);
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
                Items = order.OrderItems.Select(i =>
                {
                    configByDish.TryGetValue(i.DishId, out var cfg);
                    string? station = null;
                    if (cfg?.RobotArmId is Guid armId && armCodeById.TryGetValue(armId, out var code))
                        station = code;

                    return new ServingJobItemMessage
                    {
                        DishId = i.DishId,
                        DishName = dishNameById.TryGetValue(i.DishId, out var name) ? name : null,
                        Quantity = i.Quantity,
                        Station = station,
                        LaneCode = cfg?.LaneCode
                    };
                }).ToList()
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
