using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Application.MediatR.Tray.GetAllTrays;

internal sealed class GetAllTraysQueryHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    ILogger<GetAllTraysQueryHandler> logger
) : IQueryHandler<GetAllTraysQuery, GetAllTraysResponse>
{
    public async Task<Result<GetAllTraysResponse>> Handle(
        GetAllTraysQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var trays = await trayRepository.FindListAsync(x => !x.IsDeleted, cancellationToken);

            // "Khay đang chở đơn nào" derive từ ServingJob.TrayId (thay Tray.CurrentOrderId đã bỏ):
            // chỉ tra cho khay đang bận, map khay -> đơn của job sống mới nhất.
            var busyTrayIds = trays.Where(x => x.Status != TrayStatus.Available)
                                   .Select(x => x.Id).ToList();
            var trayToOrder = new Dictionary<Guid, Guid>();
            if (busyTrayIds.Count > 0)
            {
                var jobs = await servingJobRepository.FindListAsync(
                    x => x.TrayId != null
                         && busyTrayIds.Contains(x.TrayId.Value)
                         && x.Status != ServingJobStatus.Cancelled
                         && x.Status != ServingJobStatus.Collected,
                    cancellationToken);
                trayToOrder = jobs
                    .GroupBy(x => x.TrayId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(j => j.CreatedAtUtc).First().OrderId);
            }

            var dtos = trays
                .OrderBy(x => x.Code)
                .Select(x => new TrayDto(
                    x.Id, x.Code, x.Status.ToString(),
                    trayToOrder.TryGetValue(x.Id, out var oid) ? oid : (Guid?)null,
                    x.UpdatedAtUtc))
                .ToList();

            return Result.Success(new GetAllTraysResponse(
                trays.Count(x => x.Status == TrayStatus.Available),
                trays.Count(x => x.Status == TrayStatus.Reserved),
                trays.Count(x => x.Status == TrayStatus.InUse),
                dtos), "Trays retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing trays");
            return Result.Failure<GetAllTraysResponse>(
                Error.ServerError, "An error occurred while listing trays.");
        }
    }
}
