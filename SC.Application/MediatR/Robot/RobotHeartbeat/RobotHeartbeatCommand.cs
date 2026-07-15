using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.Robot.RobotHeartbeat;

/// <summary>
/// Nhịp tim từ edge service (hub method "Heartbeat", ~20s/lần): các trạm còn sống.
/// Chỉ set LastHeartbeatUtc + Offline->Idle — KHÔNG ghi RobotEventLogs (tránh spam
/// ~4k dòng/ngày/tay), KHÔNG đụng Busy/Error/Maintenance (trạng thái nghiệp vụ giữ nguyên).
/// </summary>
public sealed record RobotHeartbeatCommand(IReadOnlyList<string> Stations)
    : ICommand<RobotHeartbeatResponse>;

public sealed record RobotHeartbeatResponse(int Updated);
