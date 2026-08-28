using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.RobotEventLog.Enum;
using RobotEventLogEntity = SC.Domain.Domain.RobotEventLog.Entity.RobotEventLog;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;
using DishAggregateRoot = SC.Domain.Domain.Dish.AggregateRoot.Dish;

namespace SC.Application.MediatR.Order.GetOrderEvents;

internal sealed class GetOrderEventsQueryHandler(
    IGenericRepository<RobotEventLogEntity, Guid> robotEventLogRepository,
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IGenericRepository<DishAggregateRoot, Guid> dishRepository,
    ILogger<GetOrderEventsQueryHandler> logger
) : IQueryHandler<GetOrderEventsQuery, GetOrderEventsResponse>
{
    public async Task<Result<GetOrderEventsResponse>> Handle(
        GetOrderEventsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Lọc theo loại sự kiện (tuỳ chọn). Type sai/không parse được -> bỏ lọc (trả tất cả).
            RobotEventType? typeFilter = null;
            if (!string.IsNullOrWhiteSpace(request.Type)
                && Enum.TryParse<RobotEventType>(request.Type.Trim(), true, out var parsed))
                typeFilter = parsed;

            // RobotEventLog gắn trực tiếp OrderId -> gom mọi job của đơn 1 lần (kể cả các lượt requeue).
            var events = await robotEventLogRepository.FindListAsync(
                x => x.OrderId == request.OrderId && !x.IsDeleted, cancellationToken);

            var filtered = (typeFilter.HasValue
                    ? events.Where(x => x.EventType == typeFilter.Value)
                    : events)
                .OrderBy(x => x.OccurredAtUtc)
                .ToList();

            // Enrich tên món + mã tay để đọc "PlaceCompleted — Cơm trắng — S1" thay vì GUID trần.
            var dishIds = filtered.Where(x => x.DishId.HasValue).Select(x => x.DishId!.Value).Distinct().ToList();
            var dishes = dishIds.Count == 0
                ? new List<DishAggregateRoot>()
                : await dishRepository.FindListAsync(x => dishIds.Contains(x.Id), cancellationToken);
            var dishNames = dishes.ToDictionary(x => x.Id, x => x.Name);

            var armIds = filtered.Where(x => x.RobotArmId.HasValue).Select(x => x.RobotArmId!.Value).Distinct().ToList();
            var arms = armIds.Count == 0
                ? new List<RobotArmEntity>()
                : await robotArmRepository.FindListAsync(x => armIds.Contains(x.Id), cancellationToken);
            var armCodes = arms.ToDictionary(x => x.Id, x => x.Code);

            var dtos = filtered.Select(x => new OrderEventDto(
                x.ServingJobId,
                x.EventType.ToString(),
                x.DishId,
                x.DishId.HasValue && dishNames.TryGetValue(x.DishId.Value, out var dn) ? dn : null,
                x.RobotArmId,
                x.RobotArmId.HasValue && armCodes.TryGetValue(x.RobotArmId.Value, out var ac) ? ac : null,
                x.Message,
                x.OccurredAtUtc)).ToList();

            return Result.Success(
                new GetOrderEventsResponse(request.OrderId, dtos.Count, dtos),
                "Lấy lịch sử sự kiện của đơn thành công.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing events for order {OrderId}", request.OrderId);
            return Result.Failure<GetOrderEventsResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi lấy lịch sử sự kiện của đơn.");
        }
    }
}
