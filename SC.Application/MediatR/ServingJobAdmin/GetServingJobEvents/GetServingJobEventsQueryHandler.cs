using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.ServingJobAdmin.GetServingJobEvents;

internal sealed class GetServingJobEventsQueryHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetServingJobEventsQueryHandler> logger
) : IQueryHandler<GetServingJobEventsQuery, GetServingJobEventsResponse>
{
    public async Task<Result<GetServingJobEventsResponse>> Handle(
        GetServingJobEventsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.JobId && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<GetServingJobEventsResponse>(
                    Error.ServingJobNotFound, "Không tìm thấy công việc phục vụ.");
            }

            var events = await robotEventLogRepository.FindListAsync(
                x => x.ServingJobId == request.JobId && !x.IsDeleted, cancellationToken);
            var ordered = events.OrderBy(x => x.OccurredAtUtc).ToList();

            // Enrich món + tay để staff đọc "PickCompleted — Cơm trắng — S1" thay vì GUID trần.
            var dishIds = ordered.Where(x => x.DishId.HasValue).Select(x => x.DishId!.Value).Distinct().ToList();
            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNames = dishes.ToDictionary(x => x.Id, x => x.Name);

            var armIds = ordered.Where(x => x.RobotArmId.HasValue).Select(x => x.RobotArmId!.Value).Distinct().ToList();
            var arms = armIds.Count == 0
                ? new List<RobotArmEntity>()
                : await robotArmRepository.FindListAsync(x => armIds.Contains(x.Id), cancellationToken);
            var armCodes = arms.ToDictionary(x => x.Id, x => x.Code);

            var dtos = ordered.Select(x => new ServingJobEventDto(
                x.EventType.ToString(),
                x.DishId,
                x.DishId.HasValue && dishNames.TryGetValue(x.DishId.Value, out var dn) ? dn : null,
                x.RobotArmId,
                x.RobotArmId.HasValue && armCodes.TryGetValue(x.RobotArmId.Value, out var ac) ? ac : null,
                x.Message,
                x.OccurredAtUtc)).ToList();

            return Result.Success(
                new GetServingJobEventsResponse(job.Id, job.OrderId, dtos.Count, dtos),
                "Lấy lịch sử sự kiện thành công.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing events for serving job {JobId}", request.JobId);
            return Result.Failure<GetServingJobEventsResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi lấy lịch sử sự kiện.");
        }
    }
}
