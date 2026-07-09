using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.GetAllRobotArms;

internal sealed class GetAllRobotArmsQueryHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    ILogger<GetAllRobotArmsQueryHandler> logger
) : IQueryHandler<GetAllRobotArmsQuery, GetAllRobotArmsResponse>
{
    public async Task<Result<GetAllRobotArmsResponse>> Handle(
        GetAllRobotArmsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arms = await robotArmRepository.FindListAsync(
                x => !x.IsDeleted, cancellationToken);

            var dtos = arms
                .OrderBy(x => x.StationIndex)
                .Select(x => new RobotArmDto(
                    x.Id, x.Code, x.Name, x.IpAddress, x.StationIndex,
                    x.Status.ToString(), x.LastHeartbeatUtc))
                .ToList();

            return Result.Success(new GetAllRobotArmsResponse(dtos), "OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing robot arms");
            return Result.Failure<GetAllRobotArmsResponse>(
                Error.ServerError, "An error occurred while listing robot arms.");
        }
    }
}
