using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.CreateSlotConfiguration;

/// <summary>
/// Manager gán món vào lane cho 1 session: món nào - lane nào - tay nào phụ trách.
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
