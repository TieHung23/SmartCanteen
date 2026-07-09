using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.DeleteRobotArm;

/// <summary>Tháo bỏ tay máy (soft-delete). Không xóa tay đang Busy.</summary>
public sealed record DeleteRobotArmCommand(Guid Id) : ICommand<DeleteRobotArmResponse>;

public sealed record DeleteRobotArmResponse(Guid Id, string Code);
