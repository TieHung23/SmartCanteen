using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Domain.RobotArm.Enum;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.DeleteRobotArm;

internal sealed class DeleteRobotArmCommandHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteRobotArmCommandHandler> logger
) : ICommandHandler<DeleteRobotArmCommand, DeleteRobotArmResponse>
{
    public async Task<Result<DeleteRobotArmResponse>> Handle(
        DeleteRobotArmCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var arm = await robotArmRepository.FindSingleAsync(
                x => x.Id == request.Id && !x.IsDeleted, cancellationToken);
            if (arm is null)
            {
                return Result.Failure<DeleteRobotArmResponse>(
                    Error.RobotArmNotFound, "Robot arm was not found.");
            }

            if (arm.Status == RobotArmStatus.Busy)
            {
                return Result.Failure<DeleteRobotArmResponse>(
                    Error.ResourceBusy, "Arm is busy; wait for the current pick to finish.");
            }

            arm.SoftDelete();
            robotArmRepository.Update(arm);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new DeleteRobotArmResponse(arm.Id, arm.Code), "Robot arm removed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting robot arm {Id}", request.Id);
            return Result.Failure<DeleteRobotArmResponse>(
                Error.ServerError, "An error occurred while deleting the robot arm.");
        }
    }
}
