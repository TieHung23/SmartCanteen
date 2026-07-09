using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SlotConfigurationEntity = SC.Domain.Domain.SlotConfiguration.Entity.SlotConfiguration;

namespace SC.Application.MediatR.SlotConfigurationAdmin.GetSlotConfigurations;

internal sealed class GetSlotConfigurationsQueryHandler(
    IGenericRepository<SlotConfigurationEntity, Guid> slotConfigurationRepository,
    ILogger<GetSlotConfigurationsQueryHandler> logger
) : IQueryHandler<GetSlotConfigurationsQuery, GetSlotConfigurationsResponse>
{
    public async Task<Result<GetSlotConfigurationsResponse>> Handle(
        GetSlotConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var configs = await slotConfigurationRepository.FindListAsync(
                x => x.SessionId == request.SessionId && !x.IsDeleted, cancellationToken);

            var dtos = configs
                .OrderBy(x => x.LaneCode)
                .Select(x => new SlotConfigurationDto(
                    x.Id, x.SessionId, x.DishId, x.LaneCode, x.Capacity, x.RobotArmId))
                .ToList();

            return Result.Success(new GetSlotConfigurationsResponse(dtos), "OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing slot configurations for session {SessionId}", request.SessionId);
            return Result.Failure<GetSlotConfigurationsResponse>(
                Error.ServerError, "An error occurred while listing slot configurations.");
        }
    }
}
