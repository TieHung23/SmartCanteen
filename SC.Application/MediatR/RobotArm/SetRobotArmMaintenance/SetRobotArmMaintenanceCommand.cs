using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.SetRobotArmMaintenance;

/// <summary>Bật/tắt bảo trì: rút tay khỏi dây chuyền (pull sẽ né tay Maintenance).</summary>
public sealed record SetRobotArmMaintenanceCommand(
    Guid Id,
    bool InMaintenance) : ICommand<SetRobotArmMaintenanceResponse>;

public sealed record SetRobotArmMaintenanceResponse(Guid Id, string Code, string Status);
