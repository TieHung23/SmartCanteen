using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.UpdateSlotConfiguration;

/// <summary>Manager đổi món/lane/sức chứa của 1 cấu hình. Job pull SAU đó tự mang nhãn mới.</summary>
public sealed record UpdateSlotConfigurationCommand(
    Guid Id,
    Guid DishId,
    string LaneCode,
    int Capacity,
    Guid? RobotArmId) : ICommand<UpdateSlotConfigurationResponse>;

public sealed record UpdateSlotConfigurationResponse(
    Guid Id, Guid SessionId, Guid DishId, string LaneCode, int Capacity, Guid? RobotArmId);
