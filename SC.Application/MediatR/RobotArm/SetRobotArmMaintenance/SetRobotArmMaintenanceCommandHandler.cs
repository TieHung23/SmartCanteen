using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using SC.Domain.Domain.RobotArm.Enum;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.SetRobotArmMaintenance;

internal sealed class SetRobotArmMaintenanceCommandHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<SetRobotArmMaintenanceCommandHandler> logger
) : ICommandHandler<SetRobotArmMaintenanceCommand, SetRobotArmMaintenanceResponse>
{
    public async Task<Result<SetRobotArmMaintenanceResponse>> Handle(
        SetRobotArmMaintenanceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arm = await robotArmRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (arm is null)
            {
                return Result.Failure<SetRobotArmMaintenanceResponse>(
                    Error.RobotArmNotFound, "Robot arm was not found.");
            }

            if (request.InMaintenance && arm.Status == RobotArmStatus.Busy)
            {
                return Result.Failure<SetRobotArmMaintenanceResponse>(
                    Error.ResourceBusy, "Arm is busy; wait for the current pick to finish.");
            }

            arm.UpdateStatus(
                request.InMaintenance ? RobotArmStatus.Maintenance : RobotArmStatus.Idle,
                currentUserService.UserId);
            robotArmRepository.Update(arm);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new SetRobotArmMaintenanceResponse(arm.Id, arm.Code, arm.Status.ToString()),
                request.InMaintenance ? "Arm moved to maintenance." : "Arm back to service.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting maintenance for robot arm {Id}", request.Id);
            return Result.Failure<SetRobotArmMaintenanceResponse>(
                Error.ServerError, "An error occurred while updating the robot arm.");
        }
    }
}
