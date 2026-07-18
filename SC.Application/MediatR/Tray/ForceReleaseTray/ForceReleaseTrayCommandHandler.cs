using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;

namespace SC.Application.MediatR.Tray.ForceReleaseTray;

internal sealed class ForceReleaseTrayCommandHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IServingJobNotifier servingJobNotifier,
    ILogger<ForceReleaseTrayCommandHandler> logger
) : ICommandHandler<ForceReleaseTrayCommand, ForceReleaseTrayResponse>
{
    public async Task<Result<ForceReleaseTrayResponse>> Handle(
        ForceReleaseTrayCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tray = await trayRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (tray is null)
            {
                return Result.Failure<ForceReleaseTrayResponse>(
                    Error.TrayNotFound, "Tray was not found.");
            }

            if (tray.Status == TrayStatus.Available)
            {
                return Result.Success(
                    new ForceReleaseTrayResponse(tray.Id, tray.Code, tray.Status.ToString()),
                    "Tray is already available.");
            }

            // Guard: khay đang được job SỐNG dùng (robot đang/sắp gắp) -> KHÔNG cho ép trả.
            // Tìm job sống trỏ THẲNG vào khay này (thay Tray.CurrentOrderId, dùng index TrayId).
            var jobs = await servingJobRepository.FindListAsync(
                x => x.TrayId == tray.Id
                     && x.Status != ServingJobStatus.Cancelled
                     && x.Status != ServingJobStatus.Collected,
                cancellationToken);
            var activeJob = jobs.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();

            if (activeJob is not null &&
                activeJob.Status is ServingJobStatus.Pushed or ServingJobStatus.Assembling)
            {
                return Result.Failure<ForceReleaseTrayResponse>(
                    Error.ResourceBusy,
                    $"Tray is in use by an active serving job ({activeJob.Status}). " +
                    "Resolve or cancel the job first.");
            }

            // Job Failed còn trỏ vào khay -> gỡ khay khỏi job (staff đã dọn đồ khỏi khay)
            if (activeJob is not null)
            {
                activeJob.ClearTray(currentUserService.UserId);
                servingJobRepository.Update(activeJob);
            }

            tray.Release(currentUserService.UserId);
            trayRepository.Update(tray);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // khay vừa rảnh -> đánh thức robot phục vụ đơn đang chờ khay (best-effort)
            try { await servingJobNotifier.PingNewJobAsync(cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Ping after force-release failed"); }

            return Result.Success(
                new ForceReleaseTrayResponse(tray.Id, tray.Code, tray.Status.ToString()),
                "Tray force-released back to pool.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error force-releasing tray {Id}", request.Id);
            return Result.Failure<ForceReleaseTrayResponse>(
                Error.ServerError, "An error occurred while releasing the tray.");
        }
    }
}
