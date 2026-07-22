using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.GetSlotConfigurations;

/// <summary>Manager xem cấu hình món↔lane↔tay của 1 session (ca).</summary>
public sealed record GetSlotConfigurationsQuery(Guid SessionId) : IQuery<GetSlotConfigurationsResponse>;

public sealed record SlotConfigurationDto(
    Guid Id, Guid SessionId, Guid DishId, string LaneCode, int Capacity, Guid? RobotArmId);

public sealed record GetSlotConfigurationsResponse(IReadOnlyList<SlotConfigurationDto> Configurations);
