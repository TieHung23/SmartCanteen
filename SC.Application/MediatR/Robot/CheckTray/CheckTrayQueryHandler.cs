using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Robot.CheckTray;

internal sealed class CheckTrayQueryHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
    ILogger<CheckTrayQueryHandler> logger
) : IQueryHandler<CheckTrayQuery, CheckTrayResponse>
{
    public async Task<Result<CheckTrayResponse>> Handle(
        CheckTrayQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = (request.TrayCode ?? string.Empty).Trim();
            if (code.Length == 0)
                return Result.Success(
                    new CheckTrayResponse(false, null, "TrayCode is required."),
                    "TrayCode is required.");

            // Cùng tiêu chí với BindTrayCommandHandler (đăng ký? Available?) — nhưng KHÔNG reserve.
            var tray = await trayRepository.FindSingleAsync(
                x => x.Code == code && !x.IsDeleted, cancellationToken);
            if (tray is null)
                return Result.Success(
                    new CheckTrayResponse(false, null, $"Tray '{code}' chưa đăng ký."),
                    "Tray not found.");

            if (tray.Status != TrayStatus.Available)
                return Result.Success(
                    new CheckTrayResponse(false, tray.Status.ToString(),
                        $"Tray '{code}' không rảnh (status: {tray.Status})."),
                    "Tray not available.");

            return Result.Success(
                new CheckTrayResponse(true, tray.Status.ToString(), "OK"),
                "Tray is valid & available.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking tray {TrayCode}", request.TrayCode);
            return Result.Failure<CheckTrayResponse>(
                Error.ServerError, "An error occurred while checking the tray.");
        }
    }
}
