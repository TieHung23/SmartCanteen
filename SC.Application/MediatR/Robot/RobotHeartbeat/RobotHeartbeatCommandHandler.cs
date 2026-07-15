using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.Robot.RobotHeartbeat;

internal sealed class RobotHeartbeatCommandHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<RobotHeartbeatCommandHandler> logger
) : ICommandHandler<RobotHeartbeatCommand, RobotHeartbeatResponse>
{
    public async Task<Result<RobotHeartbeatResponse>> Handle(
        RobotHeartbeatCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var codes = (request.Stations ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            if (codes.Count == 0)
            {
                return Result.Success(new RobotHeartbeatResponse(0), "No stations.");
            }

            // Tram chua dang ky trong RobotArms -> bo qua im lang (edge co the quan ly nhieu tram hon BE)
            var arms = await robotArmRepository.FindListAsync(
                x => !x.IsDeleted && codes.Contains(x.Code.ToLower()), cancellationToken);

            var actorId = currentUserService.UserId;
            foreach (var arm in arms)
            {
                arm.Heartbeat(actorId);            // LastHeartbeatUtc=now; Offline->Idle (chi khi Offline)
                robotArmRepository.Update(arm);
            }

            if (arms.Count > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new RobotHeartbeatResponse(arms.Count), "Heartbeat recorded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording robot heartbeat");
            return Result.Failure<RobotHeartbeatResponse>(
                Error.ServerError, "An error occurred while recording the heartbeat.");
        }
    }
}
