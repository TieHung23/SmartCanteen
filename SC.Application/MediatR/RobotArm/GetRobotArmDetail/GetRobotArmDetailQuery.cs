using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.GetRobotArmDetail;

/// <summary>Chi tiết 1 tay máy: thông tin + các lane (món↔lane) mà tay này phục vụ.
/// Mặc định lấy mọi ca; truyền SessionId để chỉ lấy lane của ca đó.</summary>
public sealed record GetRobotArmDetailQuery(Guid Id, Guid? SessionId = null) : IQuery<GetRobotArmDetailResponse>;

public sealed record ArmLaneDto(
    Guid SlotConfigurationId,
    Guid SessionId,
    string LaneCode,
    Guid DishId,
    string? DishName,
    int Capacity);

public sealed record GetRobotArmDetailResponse(
    Guid Id,
    string Code,
    string? Name,
    string IpAddress,
    int StationIndex,
    string Status,
    DateTimeOffset? LastHeartbeatUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<ArmLaneDto> Lanes);
