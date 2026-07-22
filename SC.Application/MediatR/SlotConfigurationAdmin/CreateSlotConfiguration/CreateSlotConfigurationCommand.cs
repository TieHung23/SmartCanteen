using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.CreateSlotConfiguration;

/// <summary>
/// Manager gán món vào lane cho 1 session: món nào - lane nào - tay nào phụ trách.
/// LaneCode = mã vị trí "S1_L1".."S3_L3" (FE hardcode dropdown; BE validate LaneCatalog).
/// Ràng buộc: (SessionId, LaneCode) duy nhất (1 lane 1 món/ca).
/// </summary>
public sealed record CreateSlotConfigurationCommand(
    Guid SessionId,
    Guid DishId,
    string LaneCode,
    int Capacity,
    Guid? RobotArmId) : ICommand<CreateSlotConfigurationResponse>;

public sealed record CreateSlotConfigurationResponse(
    Guid Id, Guid SessionId, Guid DishId, string LaneCode, int Capacity, Guid? RobotArmId);
