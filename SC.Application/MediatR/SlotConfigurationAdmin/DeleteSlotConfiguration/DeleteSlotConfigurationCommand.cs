using SC.Contract.Abstraction.Message;

namespace SC.Application.MediatR.SlotConfigurationAdmin.DeleteSlotConfiguration;

/// <summary>Gỡ món khỏi lane (soft-delete cấu hình).</summary>
public sealed record DeleteSlotConfigurationCommand(Guid Id) : ICommand<DeleteSlotConfigurationResponse>;

public sealed record DeleteSlotConfigurationResponse(Guid Id, string LaneCode);
