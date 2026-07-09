using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Services.Robot;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Tray.ForceReleaseTray;

internal sealed class ForceReleaseTrayCommandHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
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
