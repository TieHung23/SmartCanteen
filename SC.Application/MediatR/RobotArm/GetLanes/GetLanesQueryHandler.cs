using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.RobotArm;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.GetLanes;

internal sealed class GetLanesQueryHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    ILogger<GetLanesQueryHandler> logger
) : IQueryHandler<GetLanesQuery, GetLanesResponse>
{
    public async Task<Result<GetLanesResponse>> Handle(
        GetLanesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arms = await robotArmRepository.FindListAsync(
                x => !x.IsDeleted, cancellationToken);

            var stations = arms
                .OrderBy(x => x.StationIndex)
                .Select(x => new StationLanesDto(x.Id, x.Code, LaneCatalog.ForStation(x.Code)))
                .ToList();

            return Result.Success(
                new GetLanesResponse(LaneCatalog.LanesPerStation, stations),
                "Lanes retrieved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing lanes");
            return Result.Failure<GetLanesResponse>(
                Error.ServerError, "An error occurred while listing lanes.");
        }
    }
}
