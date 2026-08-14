using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.ServingJob.Enum;
using SC.Domain.Domain.Tray.Enum;
using ServingJobEntity = SC.Domain.Domain.ServingJob.Entity.ServingJob;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Robot.AutoBindTray;

internal sealed class AutoBindTrayCommandHandler(
    IGenericRepository<ServingJobEntity, Guid> servingJobRepository,
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<AutoBindTrayCommandHandler> logger
) : ICommandHandler<AutoBindTrayCommand, AutoBindTrayResponse>
{
    public async Task<Result<AutoBindTrayResponse>> Handle(
        AutoBindTrayCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserService.UserId;

        try
        {
            var job = await servingJobRepository.FindSingleAsync(
                x => x.Id == request.JobId && !x.IsDeleted, cancellationToken);
            if (job is null)
            {
                return Result.Failure<AutoBindTrayResponse>(
                    Error.ServingJobNotFound, "Không tìm thấy công việc phục vụ.");
            }

            // Chỉ bind khi robot đang làm job (Pushed/Assembling) — cùng ràng buộc với BindTray.
            if (job.Status is not (ServingJobStatus.Pushed or ServingJobStatus.Assembling))
            {
                return Result.Failure<AutoBindTrayResponse>(
                    Error.ServingJobNotReady, $"Không thể gán khay cho công việc ở trạng thái {job.Status}.");
            }

            // Đã có khay (requeue) -> idempotent, trả về khay cũ, KHÔNG bind đè.
            if (job.TrayId is Guid existing)
            {
                return Result.Success(
                    new AutoBindTrayResponse(job.Id, existing, null),
                    "Công việc đã được gán khay.");
            }

            // TỰ CHỌN khay Available cũ nhất (ổn định theo Code). Hết khay -> fail.
            var trays = await trayRepository.FindListAsync(
                x => x.Status == TrayStatus.Available && !x.IsDeleted, cancellationToken);
            var tray = trays.OrderBy(x => x.Code).FirstOrDefault();
            if (tray is null)
            {
                return Result.Failure<AutoBindTrayResponse>(
                    Error.ResourceBusy, "Không có khay trống để gán.");
            }

            // Available -> Reserved + gán vào job. 1 SaveChanges = atomic.
            tray.Reserve(actorId);
            trayRepository.Update(tray);
            job.AssignTray(tray.Id, actorId);
            servingJobRepository.Update(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new AutoBindTrayResponse(job.Id, tray.Id, tray.Code),
                "Đã tự động gán khay cho công việc.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-binding tray to job {JobId}", request.JobId);
            return Result.Failure<AutoBindTrayResponse>(
                Error.ServerError, "Đã xảy ra lỗi khi tự động gán khay.");
        }
    }
}
