using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.UpdateRobotArm;

internal sealed class UpdateRobotArmCommandHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<UpdateRobotArmCommandHandler> logger
) : ICommandHandler<UpdateRobotArmCommand, UpdateRobotArmResponse>
{
    public async Task<Result<UpdateRobotArmResponse>> Handle(
        UpdateRobotArmCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ip = request.IpAddress.Trim();
            if (!System.Net.IPAddress.TryParse(ip, out _) || request.StationIndex < 0)
            {
                return Result.Failure<UpdateRobotArmResponse>(
                    Error.InvalidValue, "IpAddress/StationIndex is invalid.");
            }

            var arm = await robotArmRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (arm is null)
            {
                return Result.Failure<UpdateRobotArmResponse>(
                    Error.RobotArmNotFound, "Robot arm was not found.");
            }

            arm.Update(request.Name?.Trim(), ip, request.StationIndex, currentUserService.UserId);
            robotArmRepository.Update(arm);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new UpdateRobotArmResponse(arm.Id, arm.Code, arm.Name, arm.IpAddress,
                    arm.StationIndex, arm.Status.ToString()),
                "Robot arm updated.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating robot arm {Id}", request.Id);
            return Result.Failure<UpdateRobotArmResponse>(
                Error.ServerError, "An error occurred while updating the robot arm.");
        }
    }
}
