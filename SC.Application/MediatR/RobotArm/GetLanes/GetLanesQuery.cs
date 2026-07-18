using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.RobotArm.GetLanes;

/// <summary>FE dropdown chọn lane khi cấu hình SlotConfiguration: mỗi trạm sống trả về danh sách lane.</summary>
public sealed record GetLanesQuery : IQuery<GetLanesResponse>;

public sealed record StationLanesDto(
    Guid ArmId,
    string ArmCode,
    IReadOnlyList<string> Lanes);

public sealed record GetLanesResponse(
    int LanesPerStation,
    IReadOnlyList<StationLanesDto> Stations);
