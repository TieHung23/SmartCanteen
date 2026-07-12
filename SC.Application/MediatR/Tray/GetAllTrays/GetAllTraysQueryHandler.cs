using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.Tray.Enum;
using TrayEntity = SC.Domain.Domain.Tray.Entity.Tray;

namespace SC.Application.MediatR.Tray.GetAllTrays;

internal sealed class GetAllTraysQueryHandler(
    IGenericRepository<TrayEntity, Guid> trayRepository,
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

            var dtos = trays
                .OrderBy(x => x.Code)
                .Select(x => new TrayDto(x.Id, x.Code, x.Status.ToString(), x.CurrentOrderId, x.UpdatedAtUtc))
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
