using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.ServingJob.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using OrderAggregateRoot = SC.Domain.Domain.Order.AggregateRoot.Order;

namespace SC.Application.MediatR.Tray.GetTrayDetail;

internal sealed class GetTrayDetailQueryHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<OrderAggregateRoot, Guid> orderRepository,
    ILogger<GetTrayDetailQueryHandler> logger
) : IQueryHandler<GetTrayDetailQuery, GetTrayDetailResponse>
{
    private const int HistoryTake = 10;

    public async Task<Result<GetTrayDetailResponse>> Handle(
        GetTrayDetailQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tray = await trayRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (tray is null)
            {
                return Result.Failure<GetTrayDetailResponse>(
                    Error.TrayNotFound, "Tray was not found.");
            }

            // Mọi job từng dùng khay này (index IX_ServingJobs_TrayId)
            var jobs = await servingJobRepository.FindListAsync(
                x => x.TrayId == tray.Id, cancellationToken);

            var recent = jobs.OrderByDescending(x => x.CreatedAtUtc).Take(HistoryTake).ToList();

            // Trạng thái đơn (để FE hiện "đơn đang ở đâu" thay vì GUID trần)
            var orderIds = recent.Select(x => x.OrderId).Distinct().ToList();
            var orders = orderIds.Count == 0
                ? new List<OrderAggregateRoot>()
                : await orderRepository.FindListAsync(x => orderIds.Contains(x.Id), cancellationToken);
            var orderStatusById = orders.ToDictionary(x => x.Id, x => x.Status.ToString());

            TrayJobDto Map(ServingJobEntity j) => new(
                j.Id,
                j.OrderId,
                j.Status.ToString(),
                orderStatusById.TryGetValue(j.OrderId, out var os) ? os : null,
                j.FailureReason,
                j.CreatedAtUtc,
                j.PushedAtUtc,
                j.CompletedAtUtc);

            // Job SỐNG đang giữ khay (khay bận vì job này)
            var current = recent.FirstOrDefault(x =>
                x.Status != ServingJobStatus.Cancelled &&
                x.Status != ServingJobStatus.Collected);

            return Result.Success(
                new GetTrayDetailResponse(
                    tray.Id,
                    tray.Code,
                    tray.Status.ToString(),
                    tray.CreatedAtUtc,
                    tray.UpdatedAtUtc,
                    current is null ? null : Map(current),
                    recent.Select(Map).ToList()),
                "Tray detail retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tray detail {Id}", request.Id);
            return Result.Failure<GetTrayDetailResponse>(
                Error.ServerError, "An error occurred while getting the tray detail.");
        }
    }
}
