using Microsoft.Extensions.Logging;
using SC.Contract.Abstraction.Message;
using SC.Contract.Shared;
using SC.Domain.Abstraction.Repositories;
using SC.Domain.Abstraction.Services;
using RobotArmEntity = SC.Domain.Domain.RobotArm.Entity.RobotArm;

namespace SC.Application.MediatR.RobotArm.CreateRobotArm;

internal sealed class CreateRobotArmCommandHandler(
    IGenericRepository<RobotArmEntity, Guid> robotArmRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateRobotArmCommandHandler> logger
) : ICommandHandler<CreateRobotArmCommand, CreateRobotArmResponse>
{
    public async Task<Result<CreateRobotArmResponse>> Handle(
        CreateRobotArmCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = request.Code.Trim();
            var ip = request.IpAddress.Trim();

            if (code.Length == 0 || ip.Length == 0 || request.StationIndex < 0)
            {
                return Result.Failure<CreateRobotArmResponse>(
                    Error.InvalidValue, "Code, IpAddress and StationIndex are required.");
            }

            if (!System.Net.IPAddress.TryParse(ip, out _))
            {
                return Result.Failure<CreateRobotArmResponse>(
                    Error.InvalidValue, "IpAddress is not a valid IP address.");
            }

            var existing = await robotArmRepository.FindSingleAsync(
                x => !x.IsDeleted && x.Code.ToLower() == code.ToLower(), cancellationToken);
            if (existing is not null)
            {
                return Result.Failure<CreateRobotArmResponse>(
                    Error.CodeAlreadyExists, $"Robot arm code '{code}' already exists.");
            }

            var arm = RobotArmEntity.Create(
                code, ip, request.StationIndex, currentUserService.UserId, request.Name?.Trim());

            await robotArmRepository.AddAsync(arm, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new CreateRobotArmResponse(arm.Id, arm.Code, arm.Name, arm.IpAddress,
                    arm.StationIndex, arm.Status.ToString()),
                "Robot arm registered.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating robot arm {Code}", request.Code);
            return Result.Failure<CreateRobotArmResponse>(
                Error.ServerError, "An error occurred while creating the robot arm.");
        }
    }
}
