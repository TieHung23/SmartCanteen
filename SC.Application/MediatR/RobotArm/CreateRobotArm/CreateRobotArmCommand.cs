using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.CreateRobotArm;

/// <summary>Manager đăng ký tay máy mới (Code unique, vd S1/S2/S3).</summary>
public sealed record CreateRobotArmCommand(
    string Code,
    string IpAddress,
    int StationIndex,
    string? Name) : ICommand<CreateRobotArmResponse>;

public sealed record CreateRobotArmResponse(
    Guid Id, string Code, string? Name, string IpAddress, int StationIndex, string Status);
