using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Tray.RetireTray;

internal sealed class RetireTrayCommandHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    IUnitOfWork unitOfWork,
    ILogger<RetireTrayCommandHandler> logger
) : ICommandHandler<RetireTrayCommand, RetireTrayResponse>
{
    public async Task<Result<RetireTrayResponse>> Handle(
        RetireTrayCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tray = await trayRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (tray is null)
            {
                return Result.Failure<RetireTrayResponse>(
                    Error.TrayNotFound, "Tray was not found.");
            }

            if (tray.Status != TrayStatus.Available)
            {
                return Result.Failure<RetireTrayResponse>(
                    Error.ResourceBusy,
                    "Tray is serving an order. Force-release it first if it is stuck.");
            }

            tray.SoftDelete();
            trayRepository.Update(tray);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new RetireTrayResponse(tray.Id, tray.Code), "Tray retired.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retiring tray {Id}", request.Id);
            return Result.Failure<RetireTrayResponse>(
                Error.ServerError, "An error occurred while retiring the tray.");
        }
    }
}
