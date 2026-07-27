using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.GetSlotConfigurationDetail;

/// <summary>Chi tiết 1 lane (cấu hình món↔lane↔tay của 1 ca), kèm tên món/tay/ca cho FE hiển thị.</summary>
public sealed record GetSlotConfigurationDetailQuery(Guid Id) : IQuery<GetSlotConfigurationDetailResponse>;

public sealed record GetSlotConfigurationDetailResponse(
    Guid Id,
    Guid SessionId,
    string? SessionName,
    Guid DishId,
    string? DishName,
    string LaneCode,
    int Capacity,
    Guid? RobotArmId,
    string? RobotArmCode,
    string? RobotArmName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
