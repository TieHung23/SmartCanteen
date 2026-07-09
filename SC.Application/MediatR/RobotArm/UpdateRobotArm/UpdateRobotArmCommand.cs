using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.UpdateRobotArm;

/// <summary>Manager sửa thông tin tay máy (tên/IP/trạm). Code không đổi (định danh).</summary>
public sealed record UpdateRobotArmCommand(
    Guid Id,
    string IpAddress,
    int StationIndex,
    string? Name) : ICommand<UpdateRobotArmResponse>;

public sealed record UpdateRobotArmResponse(
    Guid Id, string Code, string? Name, string IpAddress, int StationIndex, string Status);
