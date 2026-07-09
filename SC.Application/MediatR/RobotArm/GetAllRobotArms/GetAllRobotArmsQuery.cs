using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.GetAllRobotArms;

/// <summary>Manager xem danh sách tay máy + trạng thái sống/chết (dashboard).</summary>
public sealed record GetAllRobotArmsQuery : IQuery<GetAllRobotArmsResponse>;

public sealed record RobotArmDto(
    Guid Id,
    string Code,
    string? Name,
    string IpAddress,
    int StationIndex,
    string Status,
    DateTimeOffset? LastHeartbeatUtc);

public sealed record GetAllRobotArmsResponse(IReadOnlyList<RobotArmDto> Arms);
