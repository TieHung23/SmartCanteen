using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.ServingJob.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.ServingJobAdmin.GetServingJobs;

internal sealed class GetServingJobsQueryHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    ILogger<GetServingJobsQueryHandler> logger
) : IQueryHandler<GetServingJobsQuery, GetServingJobsResponse>
{
    public async Task<Result<GetServingJobsResponse>> Handle(
        GetServingJobsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            ServingJobStatus? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!Enum.TryParse<ServingJobStatus>(request.Status.Trim(), true, out var parsed))
                {
                    return Result.Failure<GetServingJobsResponse>(
                        Error.InvalidValue,
                        $"Status must be one of: {string.Join(", ", Enum.GetNames<ServingJobStatus>())}.");
                }
                status = parsed;
            }

            var take = Math.Clamp(request.Take, 1, 200);

            var jobs = await servingJobRepository.FindListAsync(
                x => !x.IsDeleted && (status == null || x.Status == status), cancellationToken);

            var page = jobs
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .ToList();

            // map TrayId -> Code để staff nhìn mã khay thay vì Guid
            var trayIds = page.Where(x => x.TrayId.HasValue).Select(x => x.TrayId!.Value).Distinct().ToList();
            List<TrayEntity> trays = trayIds.Count == 0
                ? new List<TrayEntity>()
                : await trayRepository.FindListAsync(x => trayIds.Contains(x.Id), cancellationToken);
            var trayCodes = trays.ToDictionary(x => x.Id, x => x.Code);

            var dtos = page.Select(x => new ServingJobDto(
                x.Id, x.OrderId, x.Status.ToString(),
                x.TrayId, x.TrayId.HasValue && trayCodes.TryGetValue(x.TrayId.Value, out var c) ? c : null,
                x.PickupSlotId, x.FailureReason,
                x.CreatedAtUtc, x.PushedAtUtc, x.AcknowledgedAtUtc, x.CompletedAtUtc)).ToList();

            return Result.Success(
                new GetServingJobsResponse(jobs.Count, dtos),
                "Serving jobs retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing serving jobs");
            return Result.Failure<GetServingJobsResponse>(
                Error.ServerError, "An error occurred while listing serving jobs.");
        }
    }
}
