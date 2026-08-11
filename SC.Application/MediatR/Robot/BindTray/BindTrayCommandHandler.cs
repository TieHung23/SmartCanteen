using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Robot.BindTray;

internal sealed class BindTrayCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<BindTrayCommandHandler> logger
) : ICommandHandler<BindTrayCommand, BindTrayResponse>
{
    public async Task<Result<BindTrayResponse>> Handle(
        BindTrayCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var code = (request.TrayCode ?? string.Empty).Trim();
            if (code.Length == 0)
            {
                return Result.Failure<BindTrayResponse>(
                    Error.InvalidValue, "TrayCode is required.");
            }

            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.JobId && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<BindTrayResponse>(
                    Error.ServingJobNotFound, "Serving job was not found.");
            }

            // Chỉ bind khi robot đang làm job (Pushed/Assembling).
            if (job.Status is not (ServingJobStatus.Pushed or ServingJobStatus.Assembling))
            {
                return Result.Failure<BindTrayResponse>(
                    Error.ServingJobNotReady, $"Cannot bind a tray to a {job.Status} job.");
            }

            // Đã có khay (vd requeue) -> không bind đè (đồ đã gắp còn trên khay cũ).
            if (job.TrayId is not null)
            {
                return Result.Failure<BindTrayResponse>(
                    Error.ServingJobNotReady, "Job already has a tray bound.");
            }

            var tray = await trayRepository.FindSingleAsync(
                x => x.Code == code && !x.IsDeleted, cancellationToken);
            if (tray is null)
            {
                return Result.Failure<BindTrayResponse>(
                    Error.TrayNotFound, "Tray was not found.");
            }

            if (tray.Status != TrayStatus.Available)
            {
                return Result.Failure<BindTrayResponse>(
                    Error.ResourceBusy, $"Tray '{code}' is not available (status: {tray.Status}).");
            }

            // Available -> Reserved + gán vào job. 1 SaveChanges = atomic.
            tray.Reserve(actorId);
            trayRepository.Update(tray);
            job.AssignTray(tray.Id, actorId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new BindTrayResponse(job.Id, job.OrderId, tray.Id, tray.Code, job.Status.ToString()),
                "Tray bound to job.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error binding tray to job {JobId}", request.JobId);
            return Result.Failure<BindTrayResponse>(
                Error.ServerError, "An error occurred while binding the tray.");
        }
    }
}
